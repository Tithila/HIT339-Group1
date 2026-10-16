using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LibraryManagementSystem.Controllers;

public class KioskController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFineCalculator _fineCalculator;
    private readonly IBorrowerEligibilityService _borrowerEligibilityService;
    private readonly IAuditService _auditService;
    private readonly IPaymentService _paymentService;
    private readonly SquareSettings _squareSettings;

    private const string KioskBorrowerSessionKey = "KioskBorrowerId";
    private const string KioskFineLoanSessionKey = "KioskFineLoanId";

    public KioskController(
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

        var cardNumber =
            model.LibraryCardNumber.Trim();

        var borrower = await _context.Borrowers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                b => b.LibraryCardNumber == cardNumber);

        model.SearchPerformed = true;
        model.Borrower = borrower;

        if (borrower != null &&
            !borrower.IsSuspended)
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
        var borrower =
            await GetVerifiedBorrowerAsync();

        if (borrower == null)
        {
            return RedirectToAction(
                nameof(Borrower));
        }

        var model =
            new KioskCheckoutViewModel
            {
                Borrower = borrower
            };

        return View(model);
    }

    // POST: /Kiosk
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        KioskCheckoutViewModel model)
    {
        var borrower =
            await GetVerifiedBorrowerAsync();

        if (borrower == null)
        {
            return RedirectToAction(
                nameof(Borrower));
        }

        model.Borrower = borrower;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var code =
            model.LibraryCode.Trim();

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
        var borrower =
            await GetVerifiedBorrowerAsync();

        if (borrower == null)
        {
            return RedirectToAction(
                nameof(Borrower));
        }

        var item = await _context.Items
            .FirstOrDefaultAsync(
                i => i.LibraryCode == libraryCode);

        if (item == null)
        {
            TempData["KioskError"] =
                "The item could not be found.";

            return RedirectToAction(
                nameof(Index));
        }

        if (item.Status != ItemStatus.Available)
        {
            TempData["KioskError"] =
                "This item is not currently available for checkout.";

            return RedirectToAction(
                nameof(Index));
        }

        if (borrower.IsSuspended)
        {
            HttpContext.Session.Remove(
                KioskBorrowerSessionKey);

            TempData["KioskError"] =
                "This borrower is suspended and cannot borrow items.";

            return RedirectToAction(
                nameof(Borrower));
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

        var model =
            new KioskCheckoutViewModel
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
        // Starting a new return clears any previous
        // fine-payment session.
        HttpContext.Session.Remove(
            KioskFineLoanSessionKey);

        return View(
            new KioskReturnViewModel());
    }

    // POST: /Kiosk/Return
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(
        KioskReturnViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var code =
            model.LibraryCode.Trim();

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
            .FirstOrDefaultAsync(
                l =>
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
        if (string.IsNullOrWhiteSpace(
            libraryCode))
        {
            TempData["KioskReturnError"] =
                "A library code is required.";

            return RedirectToAction(
                nameof(Return));
        }

        var item = await _context.Items
            .FirstOrDefaultAsync(
                i => i.LibraryCode ==
                     libraryCode);

        if (item == null)
        {
            TempData["KioskReturnError"] =
                "The item could not be found.";

            return RedirectToAction(
                nameof(Return));
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
            TempData["KioskReturnError"] =
                "No active loan was found for this item.";

            return RedirectToAction(
                nameof(Return));
        }

        loan.ReturnDate = DateTime.Now;

        loan.IsOverdue =
            loan.ReturnDate.Value.Date >
            loan.DueDate.Date;

        loan.FineAmount =
            await _fineCalculator
                .CalculateFineAsync(loan);

        // Fine is considered paid automatically only
        // when there is no fine.
        loan.FinePaid =
            loan.FineAmount <= 0;

        item.Status =
            ItemStatus.Available;

        await _context.SaveChangesAsync();

        await _borrowerEligibilityService
            .CheckAndUpdateSuspensionAsync(
                loan.BorrowerId);

        await _auditService.LogAsync(
            "Kiosk Return",
            "Loan",
            loan.Id,
            $"{item.Name} ({item.LibraryCode}) returned through " +
            $"the self-service kiosk by {loan.Borrower?.FullName}. " +
            $"Fine: ${loan.FineAmount:F2}.",
            loan.Borrower?.LibraryCardNumber
                ?? "Self-Service Kiosk");

        // If money is owed, remember which loan is
        // allowed to be paid from this kiosk session.
        if (loan.FineAmount > 0 &&
            !loan.FinePaid)
        {
            HttpContext.Session.SetInt32(
                KioskFineLoanSessionKey,
                loan.Id);
        }
        else
        {
            HttpContext.Session.Remove(
                KioskFineLoanSessionKey);
        }

        var model =
            new KioskReturnViewModel
            {
                LibraryCode =
                    item.LibraryCode,

                Item = item,

                Loan = loan,

                ItemScanned = true,

                ReturnCompleted = true,

                ReturnDate =
                    loan.ReturnDate,

                FineAmount =
                    loan.FineAmount
            };

        return View(
            "Return",
            model);
    }

    // =========================================================
    // SQUARE FINE PAYMENT
    // =========================================================

    // GET: /Kiosk/PayFine?loanId=15
    [HttpGet]
    public async Task<IActionResult> PayFine(
        int loanId)
    {
        var allowedLoanId =
            HttpContext.Session.GetInt32(
                KioskFineLoanSessionKey);

        if (allowedLoanId == null ||
            allowedLoanId.Value != loanId)
        {
            TempData["KioskReturnError"] =
                "The fine payment session is no longer available.";

            return RedirectToAction(
                nameof(Return));
        }

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

        if (loan.FineAmount <= 0)
        {
            TempData["KioskReturnError"] =
                "This loan does not have an outstanding fine.";

            return RedirectToAction(
                nameof(Return));
        }

        if (loan.FinePaid)
        {
            TempData["KioskReturnError"] =
                "This fine has already been paid.";

            return RedirectToAction(
                nameof(Return));
        }

        var model =
            BuildFinePaymentViewModel(
                loan);

        return View(model);
    }

    // POST: /Kiosk/ProcessFinePayment
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        ProcessFinePayment(
            int loanId,
            string sourceId)
    {
        var allowedLoanId =
            HttpContext.Session.GetInt32(
                KioskFineLoanSessionKey);

        if (allowedLoanId == null ||
            allowedLoanId.Value != loanId)
        {
            return BadRequest(
                new
                {
                    success = false,
                    message =
                        "The payment session has expired."
                });
        }

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

        HttpContext.Session.Remove(
            KioskFineLoanSessionKey);

        return Ok(
            new
            {
                success = true,
                paymentId =
                    result.PaymentId
            });
    }

    // GET: /Kiosk/PaymentSuccess
    [HttpGet]
    public async Task<IActionResult>
        PaymentSuccess(
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
            return RedirectToAction(
                nameof(Return));
        }

        var model =
            BuildFinePaymentViewModel(
                loan);

        model.PaymentCompleted = true;
        model.PaymentId = paymentId;

        return View(
            "PayFine",
            model);
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

        HttpContext.Session.Remove(
            KioskFineLoanSessionKey);

        return RedirectToAction(
            nameof(Borrower));
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private async Task<Borrower?>
        GetVerifiedBorrowerAsync()
    {
        var borrowerId =
            HttpContext.Session.GetInt32(
                KioskBorrowerSessionKey);

        if (borrowerId == null)
        {
            return null;
        }

        var borrower =
            await _context.Borrowers
                .FirstOrDefaultAsync(
                    b =>
                        b.Id ==
                        borrowerId.Value);

        if (borrower == null ||
            borrower.IsSuspended)
        {
            HttpContext.Session.Remove(
                KioskBorrowerSessionKey);

            return null;
        }

        return borrower;
    }

    private KioskFinePaymentViewModel
        BuildFinePaymentViewModel(
            Loan loan)
    {
        return new KioskFinePaymentViewModel
        {
            LoanId = loan.Id,

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