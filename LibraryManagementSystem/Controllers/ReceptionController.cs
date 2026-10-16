using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LibraryManagementSystem.Controllers;

[Authorize(Roles = "Reception")]
public class ReceptionController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFineCalculator _fineCalculator;
    private readonly IBorrowerEligibilityService _borrowerEligibilityService;
    private readonly IAuditService _auditService;
    private readonly IPaymentService _paymentService;
    private readonly SquareSettings _squareSettings;

    public ReceptionController(
        ApplicationDbContext context,
        IFineCalculator fineCalculator,
        IBorrowerEligibilityService borrowerEligibilityService,
        IAuditService auditService,
        IPaymentService paymentService,
        IOptions<SquareSettings> squareOptions)
    {
        _context = context;
        _fineCalculator = fineCalculator;
        _borrowerEligibilityService = borrowerEligibilityService;
        _auditService = auditService;
        _paymentService = paymentService;
        _squareSettings = squareOptions.Value;
    }

    // =========================================================
    // BORROW
    // =========================================================

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
    public async Task<IActionResult> Borrow(
        BorrowItemViewModel model)
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
            .FirstOrDefaultAsync(
                i => i.LibraryCode == model.LibraryCode);

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
            .FirstOrDefaultAsync(
                b => b.Id == model.BorrowerId);

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

    // =========================================================
    // RETURN
    // =========================================================

    // GET: Reception/Return
    public IActionResult Return()
    {
        return View(new ReturnItemViewModel());
    }

    // POST: Reception/Return
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(
        ReturnItemViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var item = await _context.Items
            .FirstOrDefaultAsync(
                i => i.LibraryCode == model.LibraryCode);

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
            .FirstOrDefaultAsync(
                l =>
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
            loan.ReturnDate.Value.Date >
            loan.DueDate.Date;

        loan.FineAmount =
            await _fineCalculator
                .CalculateFineAsync(loan);

        loan.FinePaid =
            loan.FineAmount <= 0;

        item.Status = model.Condition switch
        {
            ItemConditionResult.Good =>
                ItemStatus.Available,

            ItemConditionResult.Damaged =>
                ItemStatus.Damaged,

            ItemConditionResult.Destroyed =>
                ItemStatus.Destroy,

            _ =>
                ItemStatus.Available
        };

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

        await _borrowerEligibilityService
            .CheckAndUpdateSuspensionAsync(
                loan.BorrowerId);

        await _auditService.LogAsync(
            "Return",
            "Loan",
            loan.Id,
            $"{item.Name} ({item.LibraryCode}) returned by " +
            $"{loan.Borrower?.FullName}. " +
            $"Condition: {model.Condition}. " +
            $"Fine: ${loan.FineAmount:F2}. " +
            $"Repair Cost: ${model.RepairCost:F2}. " +
            $"Replacement Cost: ${model.ReplacementCost:F2}.",
            User.Identity?.Name);

        var resultModel =
            new ReceptionReturnResultViewModel
            {
                LoanId = loan.Id,

                BorrowerName =
                    loan.Borrower?.FullName
                    ?? "Unknown",

                ItemName =
                    item.Name,

                LibraryCode =
                    item.LibraryCode,

                Condition =
                    model.Condition,

                ReturnDate =
                    loan.ReturnDate.Value,

                FineAmount =
                    loan.FineAmount,

                RepairCost =
                    model.RepairCost,

                ReplacementCost =
                    model.ReplacementCost
            };

        return View(
            "ReturnResult",
            resultModel);
    }

    // =========================================================
    // OUTSTANDING FINES
    // =========================================================

    // GET: Reception/Fines
    public async Task<IActionResult> Fines()
    {
        var fines = await _context.Loans
            .Include(l => l.Borrower)
            .Include(l => l.Item)
            .Where(
                l =>
                    l.FineAmount > 0 &&
                    !l.FinePaid)
            .OrderByDescending(l => l.ReturnDate)
            .Select(
                l => new FinePaymentViewModel
                {
                    LoanId = l.Id,

                    BorrowerName =
                        l.Borrower != null
                            ? l.Borrower.FullName
                            : "Unknown",

                    ItemName =
                        l.Item != null
                            ? l.Item.Name
                            : "Unknown",

                    LibraryCode =
                        l.Item != null
                            ? l.Item.LibraryCode
                            : string.Empty,

                    FineAmount =
                        l.FineAmount,

                    ReturnDate =
                        l.ReturnDate
                })
            .ToListAsync();

        return View(fines);
    }

    // =========================================================
    // SQUARE PAYMENT
    // =========================================================

    // GET: Reception/PayFineSquare/15
    [HttpGet]
    public async Task<IActionResult> PayFineSquare(
        int id)
    {
        var loan = await _context.Loans
            .AsNoTracking()
            .Include(l => l.Borrower)
            .Include(l => l.Item)
            .FirstOrDefaultAsync(
                l => l.Id == id);

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

        var model =
            BuildReceptionFinePaymentViewModel(
                loan);

        return View(model);
    }

    // POST: Reception/ProcessSquareFinePayment
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessSquareFinePayment(
        int loanId,
        string sourceId)
    {
        var result =
            await _paymentService.PayFineAsync(
                loanId,
                sourceId);

        if (!result.Success)
        {
            return BadRequest(
                new
                {
                    success = false,

                    message =
                        result.ErrorMessage
                        ?? "Payment failed."
                });
        }

        return Ok(
            new
            {
                success = true,

                paymentId =
                    result.PaymentId
            });
    }

    // GET: Reception/SquarePaymentSuccess
    [HttpGet]
    public async Task<IActionResult> SquarePaymentSuccess(
        int loanId,
        string? paymentId)
    {
        var loan = await _context.Loans
            .AsNoTracking()
            .Include(l => l.Borrower)
            .Include(l => l.Item)
            .FirstOrDefaultAsync(
                l => l.Id == loanId);

        if (loan == null)
        {
            return NotFound();
        }

        if (!loan.FinePaid)
        {
            TempData["ErrorMessage"] =
                "Payment has not been confirmed.";

            return RedirectToAction(nameof(Fines));
        }

        var model =
            BuildReceptionFinePaymentViewModel(
                loan);

        model.PaymentCompleted = true;
        model.PaymentId = paymentId;

        return View(
            "PayFineSquare",
            model);
    }

    // =========================================================
    // CASH / MANUAL PAYMENT
    // =========================================================

    // POST: Reception/PayFine
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayFine(
        int loanId)
    {
        var loan = await _context.Loans
            .Include(l => l.Borrower)
            .Include(l => l.Item)
            .FirstOrDefaultAsync(
                l => l.Id == loanId);

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
            .CheckAndUpdateSuspensionAsync(
                loan.BorrowerId);

        await _auditService.LogAsync(
            "Fine Paid - Manual",
            "Loan",
            loan.Id,
            $"Fine of ${loan.FineAmount:F2} manually recorded as paid " +
            $"by Reception for {loan.Borrower?.FullName}. " +
            $"Item: {loan.Item?.Name} ({loan.Item?.LibraryCode}).",
            User.Identity?.Name);

        TempData["SuccessMessage"] =
            $"Fine of ${loan.FineAmount:F2} has been recorded as paid.";

        return RedirectToAction(nameof(Fines));
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private ReceptionFinePaymentViewModel
        BuildReceptionFinePaymentViewModel(
            Loan loan)
    {
        return new ReceptionFinePaymentViewModel
        {
            LoanId =
                loan.Id,

            BorrowerName =
                loan.Borrower?.FullName
                ?? "Unknown",

            ItemName =
                loan.Item?.Name
                ?? "Unknown",

            LibraryCode =
                loan.Item?.LibraryCode
                ?? string.Empty,

            FineAmount =
                loan.FineAmount,

            SquareApplicationId =
                _squareSettings.ApplicationId,

            SquareLocationId =
                _squareSettings.LocationId
        };
    }
}