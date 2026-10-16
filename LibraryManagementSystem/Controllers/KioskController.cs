using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers;

public class KioskController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IBorrowerEligibilityService _eligibilityService;
    private readonly IFineCalculator _fineCalculator;
    private readonly IReservationService _reservationService;

    public KioskController(
        ApplicationDbContext context,
        IBorrowerEligibilityService eligibilityService,
        IFineCalculator fineCalculator,
        IReservationService reservationService)
    {
        _context = context;
        _eligibilityService = eligibilityService;
        _fineCalculator = fineCalculator;
        _reservationService = reservationService;
    }

    // GET: /Kiosk
    [HttpGet]
    public IActionResult Index()
    {
        return View(new KioskSearchViewModel());
    }

    // POST: /Kiosk
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(KioskSearchViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.LibraryCode))
        {
            model.ActionError = "Enter or scan a library code.";
            return View(model);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var code = model.LibraryCode.Trim();

        var item = await _context.Items
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.LibraryCode == code);

        model.SearchPerformed = true;
        model.Item = item;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckOut(KioskSearchViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.LibraryCode))
        {
            model.ActionError = "Enter or scan a library code before checking out an item.";
            return View("Index", model);
        }

        model.LibraryCode = model.LibraryCode.Trim();
        model.LibraryCardNumber = model.LibraryCardNumber?.Trim();
        model.SearchPerformed = true;

        var item = await _context.Items
            .FirstOrDefaultAsync(i => i.LibraryCode == model.LibraryCode);
        model.Item = item;

        if (item == null)
        {
            model.ActionError = "No item was found with that library code.";
            return View("Index", model);
        }

        if (item.Status != ItemStatus.Available)
        {
            model.ActionError = "Only available items can be checked out.";
            return View("Index", model);
        }

        if (string.IsNullOrWhiteSpace(model.LibraryCardNumber))
        {
            model.ActionError = "Enter a library card number to check out an item.";
            return View("Index", model);
        }

        var borrower = await _context.Borrowers
            .FirstOrDefaultAsync(b =>
                b.LibraryCardNumber == model.LibraryCardNumber);

        if (borrower == null)
        {
            model.ActionError = "No borrower was found with that library card number.";
            return View("Index", model);
        }

        await _eligibilityService.CheckAndUpdateSuspensionAsync(borrower.Id);

        if (borrower.IsSuspended)
        {
            model.ActionError = "This borrower is suspended and cannot check out items.";
            return View("Index", model);
        }

        var reservedForPickup = await _context.Reservations
            .AnyAsync(r =>
                r.ItemId == item.Id &&
                r.Status == ReservationStatus.ReadyForPickup);

        if (reservedForPickup)
        {
            model.ActionError = "This item is reserved for another borrower.";
            return View("Index", model);
        }

        var loan = new Loan
        {
            BorrowerId = borrower.Id,
            ItemId = item.Id,
            BorrowDate = DateTime.Now,
            DueDate = DateTime.Now.AddDays(14),
            IsOverdue = false,
            FineAmount = 0,
            FinePaid = false
        };

        item.Status = ItemStatus.Borrowed;
        _context.Loans.Add(loan);
        await _context.SaveChangesAsync();

        model.ActionMessage =
            $"{item.Name} was checked out to {borrower.FullName}. " +
            $"Due date: {loan.DueDate:dd MMM yyyy}.";

        return View("Index", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(KioskSearchViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.LibraryCode))
        {
            model.ActionError = "Enter or scan a library code before checking in an item.";
            return View("Index", model);
        }

        model.LibraryCode = model.LibraryCode.Trim();
        model.SearchPerformed = true;

        var item = await _context.Items
            .FirstOrDefaultAsync(i => i.LibraryCode == model.LibraryCode);
        model.Item = item;

        if (item == null)
        {
            model.ActionError = "No item was found with that library code.";
            return View("Index", model);
        }

        var loan = await _context.Loans
            .Include(l => l.Borrower)
            .Include(l => l.Item)
            .FirstOrDefaultAsync(l =>
                l.ItemId == item.Id && l.ReturnDate == null);

        if (loan == null)
        {
            model.ActionError = "This item does not have an active checkout.";
            return View("Index", model);
        }

        loan.ReturnDate = DateTime.Now;
        loan.IsOverdue = loan.ReturnDate.Value.Date > loan.DueDate.Date;
        loan.FineAmount = await _fineCalculator.CalculateFineAsync(loan);
        item.Status = ItemStatus.Available;

        await _context.SaveChangesAsync();
        await _reservationService.PromoteNextAsync(item.Id);

        model.ActionMessage =
            $"{item.Name} was checked in successfully. " +
            (loan.FineAmount > 0
                ? $"Fine due: ${loan.FineAmount:F2}."
                : "No fine is due.");

        return View("Index", model);
    }
}