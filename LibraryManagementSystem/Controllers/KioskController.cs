using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers;

public class KioskController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFineCalculator _fineCalculator;
    private readonly IBorrowerEligibilityService _borrowerEligibilityService;
    private readonly IAuditService _auditService;

    private const string KioskBorrowerSessionKey = "KioskBorrowerId";

    public KioskController(
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

    // =========================================================
    // BORROWER IDENTIFICATION
    // =========================================================

    // GET: /Kiosk/Borrower
    [HttpGet]
    public IActionResult Borrower()
    {
        return View(new KioskBorrowerViewModel());
    }

    // POST: /Kiosk/Borrower
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Borrower(
        KioskBorrowerViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var cardNumber = model.LibraryCardNumber.Trim();

        var borrower = await _context.Borrowers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                b => b.LibraryCardNumber == cardNumber);

        model.SearchPerformed = true;
        model.Borrower = borrower;

        if (borrower != null && !borrower.IsSuspended)
        {
            HttpContext.Session.SetInt32(
                KioskBorrowerSessionKey,
                borrower.Id);
        }
        else
        {
            HttpContext.Session.Remove(
                KioskBorrowerSessionKey);
        }

        return View(model);
    }

    // =========================================================
    // SELF-SERVICE CHECKOUT
    // =========================================================

    // GET: /Kiosk
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var borrower = await GetVerifiedBorrowerAsync();

        if (borrower == null)
        {
            return RedirectToAction(nameof(Borrower));
        }

        var model = new KioskCheckoutViewModel
        {
            Borrower = borrower
        };

        return View(model);
    }

    // POST: /Kiosk
    // Scan / look up item
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        KioskCheckoutViewModel model)
    {
        var borrower = await GetVerifiedBorrowerAsync();

        if (borrower == null)
        {
            return RedirectToAction(nameof(Borrower));
        }

        model.Borrower = borrower;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var code = model.LibraryCode.Trim();

        var item = await _context.Items
            .AsNoTracking()
            .FirstOrDefaultAsync(
                i => i.LibraryCode == code);

        model.ItemScanned = true;
        model.Item = item;

        return View(model);
    }

    // POST: /Kiosk/Checkout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(
        string libraryCode)
    {
        var borrower = await GetVerifiedBorrowerAsync();

        if (borrower == null)
        {
            return RedirectToAction(nameof(Borrower));
        }

        var item = await _context.Items
            .FirstOrDefaultAsync(
                i => i.LibraryCode == libraryCode);

        if (item == null)
        {
            TempData["KioskError"] =
                "The item could not be found.";

            return RedirectToAction(nameof(Index));
        }

        if (item.Status != ItemStatus.Available)
        {
            TempData["KioskError"] =
                "This item is not currently available for checkout.";

            return RedirectToAction(nameof(Index));
        }

        if (borrower.IsSuspended)
        {
            HttpContext.Session.Remove(
                KioskBorrowerSessionKey);

            TempData["KioskError"] =
                "This borrower is suspended and cannot borrow items.";

            return RedirectToAction(nameof(Borrower));
        }

        var borrowDate = DateTime.Now;
        var dueDate = borrowDate.AddDays(14);

        var loan = new Loan
        {
            BorrowerId = borrower.Id,
            ItemId = item.Id,
            BorrowDate = borrowDate,
            DueDate = dueDate,
            IsOverdue = false,
            FineAmount = 0,
            FinePaid = false
        };

        item.Status = ItemStatus.Borrowed;

        _context.Loans.Add(loan);

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Kiosk Checkout",
            "Loan",
            loan.Id,
            $"{item.Name} ({item.LibraryCode}) checked out by " +
            $"{borrower.FullName} using self-service kiosk.",
            borrower.LibraryCardNumber);

        var model = new KioskCheckoutViewModel
        {
            Borrower = borrower,
            Item = item,
            LibraryCode = item.LibraryCode,
            ItemScanned = true,
            CheckoutCompleted = true,
            DueDate = dueDate,
            LoanId = loan.Id
        };

        return View("Index", model);
    }

    // =========================================================
    // SELF-SERVICE RETURN
    // =========================================================

    // GET: /Kiosk/Return
    [HttpGet]
    public IActionResult Return()
    {
        return View(new KioskReturnViewModel());
    }

    // POST: /Kiosk/Return
    // Scan item and find active loan
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(
        KioskReturnViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var code = model.LibraryCode.Trim();

        var item = await _context.Items
            .AsNoTracking()
            .FirstOrDefaultAsync(
                i => i.LibraryCode == code);

        model.ItemScanned = true;
        model.Item = item;

        if (item == null)
        {
            return View(model);
        }

        var loan = await _context.Loans
            .AsNoTracking()
            .Include(l => l.Borrower)
            .Include(l => l.Item)
            .FirstOrDefaultAsync(l =>
                l.ItemId == item.Id &&
                l.ReturnDate == null);

        model.Loan = loan;

        return View(model);
    }

    // POST: /Kiosk/ConfirmReturn
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmReturn(
        string libraryCode)
    {
        if (string.IsNullOrWhiteSpace(libraryCode))
        {
            TempData["KioskReturnError"] =
                "A library code is required.";

            return RedirectToAction(nameof(Return));
        }

        var item = await _context.Items
            .FirstOrDefaultAsync(
                i => i.LibraryCode == libraryCode);

        if (item == null)
        {
            TempData["KioskReturnError"] =
                "The item could not be found.";

            return RedirectToAction(nameof(Return));
        }

        var loan = await _context.Loans
            .Include(l => l.Borrower)
            .Include(l => l.Item)
            .FirstOrDefaultAsync(l =>
                l.ItemId == item.Id &&
                l.ReturnDate == null);

        if (loan == null)
        {
            TempData["KioskReturnError"] =
                "No active loan was found for this item.";

            return RedirectToAction(nameof(Return));
        }

        // Record the return.
        loan.ReturnDate = DateTime.Now;

        loan.IsOverdue =
            loan.ReturnDate.Value.Date > loan.DueDate.Date;

        // Reuse the existing fine calculator.
        loan.FineAmount =
            await _fineCalculator.CalculateFineAsync(loan);

        // Fine remains unpaid until payment is completed.
        loan.FinePaid = loan.FineAmount <= 0;

        // A kiosk cannot perform a physical condition inspection,
        // so the item returns to available circulation.
        item.Status = ItemStatus.Available;

        await _context.SaveChangesAsync();

        // Recheck borrower eligibility after the return/fine update.
        await _borrowerEligibilityService
            .CheckAndUpdateSuspensionAsync(
                loan.BorrowerId);

        // Write an audit log entry.
        await _auditService.LogAsync(
            "Kiosk Return",
            "Loan",
            loan.Id,
            $"{item.Name} ({item.LibraryCode}) returned through " +
            $"the self-service kiosk by {loan.Borrower?.FullName}. " +
            $"Fine: ${loan.FineAmount:F2}.",
            loan.Borrower?.LibraryCardNumber ?? "Self-Service Kiosk");

        var model = new KioskReturnViewModel
        {
            LibraryCode = item.LibraryCode,
            Item = item,
            Loan = loan,
            ItemScanned = true,
            ReturnCompleted = true,
            ReturnDate = loan.ReturnDate,
            FineAmount = loan.FineAmount
        };

        return View("Return", model);
    }

    // =========================================================
    // SESSION
    // =========================================================

    // GET: /Kiosk/Cancel
    [HttpGet]
    public IActionResult Cancel()
    {
        HttpContext.Session.Remove(
            KioskBorrowerSessionKey);

        return RedirectToAction(nameof(Borrower));
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private async Task<Borrower?> GetVerifiedBorrowerAsync()
    {
        var borrowerId =
            HttpContext.Session.GetInt32(
                KioskBorrowerSessionKey);

        if (borrowerId == null)
        {
            return null;
        }

        var borrower = await _context.Borrowers
            .FirstOrDefaultAsync(
                b => b.Id == borrowerId.Value);

        if (borrower == null ||
            borrower.IsSuspended)
        {
            HttpContext.Session.Remove(
                KioskBorrowerSessionKey);

            return null;
        }

        return borrower;
    }
}