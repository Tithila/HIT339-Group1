using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace LibraryManagementSystem.Controllers;

[Authorize(Roles = "Reception")]
public class ReceptionController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFineCalculator _fineCalculator;
    private readonly IBorrowerEligibilityService _borrowerEligibilityService;
    private readonly IAuditService _auditService;

    public ReceptionController(
        ApplicationDbContext context,
        IFineCalculator fineCalculator,
        IBorrowerEligibilityService borrowerEligibilityService,
        IAuditService auditService)
    {
        _context = context;
        _fineCalculator = fineCalculator;
        _borrowerEligibilityService = borrowerEligibilityService;
        _auditService = auditService;
    }

    // GET: Reception/Borrow
    public async Task<IActionResult> Borrow()
    {
        ViewBag.Borrowers = new SelectList(
            await _context.Borrowers
                .OrderBy(b => b.FullName)
                .ToListAsync(),
            "Id",
            "FullName");

        return View(new BorrowItemViewModel());
    }

    // POST: Reception/Borrow
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Borrow(BorrowItemViewModel model)
    {
        ViewBag.Borrowers = new SelectList(
            await _context.Borrowers
                .OrderBy(b => b.FullName)
                .ToListAsync(),
            "Id",
            "FullName",
            model.BorrowerId);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var item = await _context.Items
            .FirstOrDefaultAsync(i => i.LibraryCode == model.LibraryCode);

        if (item == null)
        {
            ModelState.AddModelError(
                nameof(model.LibraryCode),
                "No item was found with this library code.");

            return View(model);
        }

        if (item.Status != ItemStatus.Available)
        {
            ModelState.AddModelError(
                nameof(model.LibraryCode),
                "This item is not currently available.");

            return View(model);
        }

        var borrower = await _context.Borrowers
            .FirstOrDefaultAsync(b => b.Id == model.BorrowerId);

        if (borrower == null)
        {
            ModelState.AddModelError(
                nameof(model.BorrowerId),
                "Borrower was not found.");

            return View(model);
        }

        if (borrower.IsSuspended)
        {
            ModelState.AddModelError(
                nameof(model.BorrowerId),
                "This borrower is currently suspended and cannot borrow items.");

            return View(model);
        }

        var loan = new Loan
        {
            BorrowerId = borrower.Id,
            ItemId = item.Id,
            BorrowDate = DateTime.Now,
            DueDate = model.DueDate,
            IsOverdue = false,
            FineAmount = 0,
            FinePaid = false
        };

        item.Status = ItemStatus.Borrowed;

        _context.Loans.Add(loan);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Borrow",
            "Loan",
            loan.Id,
            $"{item.Name} ({item.LibraryCode}) borrowed by {borrower.FullName}",
            User.Identity?.Name);

        TempData["SuccessMessage"] =
            $"{item.Name} has been borrowed by {borrower.FullName}.";

        return RedirectToAction(nameof(Borrow));
    }

    // GET: Reception/Return
    public IActionResult Return()
    {
        return View(new ReturnItemViewModel());
    }

    // POST: Reception/Return
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(ReturnItemViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var item = await _context.Items
            .FirstOrDefaultAsync(i => i.LibraryCode == model.LibraryCode);

        if (item == null)
        {
            ModelState.AddModelError(
                nameof(model.LibraryCode),
                "No item was found with this library code.");

            return View(model);
        }

        var loan = await _context.Loans
            .Include(l => l.Borrower)
            .Include(l => l.Item)
            .FirstOrDefaultAsync(l =>
                l.ItemId == item.Id &&
                l.ReturnDate == null);

        if (loan == null)
        {
            ModelState.AddModelError(
                nameof(model.LibraryCode),
                "No active loan was found for this item.");

            return View(model);
        }

        loan.ReturnDate = DateTime.Now;

        loan.IsOverdue =
            loan.ReturnDate.Value.Date > loan.DueDate.Date;

        loan.FineAmount =
            await _fineCalculator.CalculateFineAsync(loan);

        // Set item status based on return condition.
        item.Status = model.Condition switch
        {
            ItemConditionResult.Good => ItemStatus.Available,
            ItemConditionResult.Damaged => ItemStatus.Damaged,
            ItemConditionResult.Destroyed => ItemStatus.Destroy,
            _ => ItemStatus.Available
        };

        // Save the return inspection.
        var inspection = new ReturnInspection
        {
            LoanId = loan.Id,
            Result = model.Condition,
            InspectorNotes = model.InspectorNotes,
            RepairCost = model.RepairCost,
            ReplacementCost = model.ReplacementCost,
            InspectionDate = DateTime.Now
        };

        _context.ReturnInspections.Add(inspection);

        await _context.SaveChangesAsync();

        // Recheck suspension after return/fine update.
        await _borrowerEligibilityService
            .CheckAndUpdateSuspensionAsync(loan.BorrowerId);

        // Record return in Audit Log.
        await _auditService.LogAsync(
            "Return",
            "Loan",
            loan.Id,
            $"{item.Name} ({item.LibraryCode}) returned by {loan.Borrower?.FullName}. " +
            $"Condition: {model.Condition}. " +
            $"Fine: ${loan.FineAmount:F2}. " +
            $"Repair Cost: ${model.RepairCost:F2}. " +
            $"Replacement Cost: ${model.ReplacementCost:F2}.",
            User.Identity?.Name);

        TempData["SuccessMessage"] =
            $"{item.Name} returned successfully. " +
            $"Condition: {model.Condition}. " +
            $"Fine: ${loan.FineAmount:F2}.";

        return RedirectToAction(nameof(Return));
    }

    // GET: Reception/Fines
    public async Task<IActionResult> Fines()
    {
        var fines = await _context.Loans
            .Include(l => l.Borrower)
            .Include(l => l.Item)
            .Where(l => l.FineAmount > 0 && !l.FinePaid)
            .OrderByDescending(l => l.ReturnDate)
            .Select(l => new FinePaymentViewModel
            {
                LoanId = l.Id,

                BorrowerName = l.Borrower != null
                    ? l.Borrower.FullName
                    : "Unknown",

                ItemName = l.Item != null
                    ? l.Item.Name
                    : "Unknown",

                LibraryCode = l.Item != null
                    ? l.Item.LibraryCode
                    : "",

                FineAmount = l.FineAmount,
                ReturnDate = l.ReturnDate
            })
            .ToListAsync();

        return View(fines);
    }

    // POST: Reception/PayFine
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayFine(int loanId)
    {
        var loan = await _context.Loans
            .Include(l => l.Borrower)
            .Include(l => l.Item)
            .FirstOrDefaultAsync(l => l.Id == loanId);

        if (loan == null)
        {
            return NotFound();
        }

        if (loan.FineAmount <= 0)
        {
            TempData["ErrorMessage"] =
                "This loan does not have an outstanding fine.";

            return RedirectToAction(nameof(Fines));
        }

        if (loan.FinePaid)
        {
            TempData["ErrorMessage"] =
                "This fine has already been paid.";

            return RedirectToAction(nameof(Fines));
        }

        loan.FinePaid = true;

        await _context.SaveChangesAsync();

        await _borrowerEligibilityService
            .CheckAndUpdateSuspensionAsync(loan.BorrowerId);

        await _auditService.LogAsync(
            "Fine Paid",
            "Loan",
            loan.Id,
            $"Fine of ${loan.FineAmount:F2} paid by {loan.Borrower?.FullName} " +
            $"for {loan.Item?.Name} ({loan.Item?.LibraryCode})",
            User.Identity?.Name);

        TempData["SuccessMessage"] =
            $"Fine of ${loan.FineAmount:F2} has been marked as paid.";

        return RedirectToAction(nameof(Fines));
    }
}