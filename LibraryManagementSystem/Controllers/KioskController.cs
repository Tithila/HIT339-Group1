using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers;

public class KioskController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    private const string KioskBorrowerSessionKey = "KioskBorrowerId";

    public KioskController(
        ApplicationDbContext context,
        IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    // -------------------------
    // Borrower identification
    // GET: /Kiosk/Borrower
    // -------------------------

    [HttpGet]
    public IActionResult Borrower()
    {
        return View(new KioskBorrowerViewModel());
    }

    // -------------------------
    // Borrower identification
    // POST: /Kiosk/Borrower
    // -------------------------

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

    // -------------------------
    // Checkout screen
    // GET: /Kiosk
    // -------------------------

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

    // -------------------------
    // Scan / look up item
    // POST: /Kiosk
    // -------------------------

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
            .FirstOrDefaultAsync(i => i.LibraryCode == code);

        model.ItemScanned = true;
        model.Item = item;

        return View(model);
    }

    // -------------------------
    // Confirm checkout
    // POST: /Kiosk/Checkout
    // -------------------------

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

    // -------------------------
    // End kiosk session
    // GET: /Kiosk/Cancel
    // -------------------------

    [HttpGet]
    public IActionResult Cancel()
    {
        HttpContext.Session.Remove(
            KioskBorrowerSessionKey);

        return RedirectToAction(nameof(Borrower));
    }

    // -------------------------
    // Helper method
    // -------------------------

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

        if (borrower == null || borrower.IsSuspended)
        {
            HttpContext.Session.Remove(
                KioskBorrowerSessionKey);

            return null;
        }

        return borrower;
    }
}