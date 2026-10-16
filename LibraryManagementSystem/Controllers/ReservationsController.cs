
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers;

[Authorize(Roles = "Reception,Admin,Manager")]
public class ReservationsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IReservationService _reservationService;

    public ReservationsController(
        ApplicationDbContext context,
        IReservationService reservationService)
    {
        _context = context;
        _reservationService = reservationService;
    }

    // GET: Reservations
    public async Task<IActionResult> Index()
    {
        var reservations = await _context.Reservations
            .AsNoTracking()
            .Include(r => r.Item)
            .Include(r => r.Borrower)
            .OrderByDescending(r => r.RequestedDate)
            .ToListAsync();

        return View(reservations);
    }

    // GET: Reservations/Create
    public async Task<IActionResult> Create()
    {
        await LoadDropdownsAsync();
        return View();
    }

    // POST: Reservations/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        int itemId,
        int borrowerId)
    {
        try
        {
            await _reservationService.PlaceReservationAsync(
                itemId, borrowerId);

            TempData["SuccessMessage"] =
                "Reservation added to the waiting list.";

            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await LoadDropdownsAsync(itemId, borrowerId);

            return View();
        }
    }

    // GET: Reservations/Waitlist/5
    public async Task<IActionResult> Waitlist(int id)
    {
        var item = await _context.Items
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);

        if (item == null)
            return NotFound();

        ViewBag.ItemName = item.Name;

        var waitlist =
            await _reservationService.GetWaitlistAsync(id);

        return View(waitlist);
    }

    // POST: Reservations/Cancel
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        bool cancelled =
            await _reservationService.CancelReservationAsync(id);

        TempData[cancelled
            ? "SuccessMessage"
            : "ErrorMessage"] = cancelled
                ? "Reservation cancelled successfully."
                : "Reservation could not be cancelled.";

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadDropdownsAsync(
        int? selectedItemId = null,
        int? selectedBorrowerId = null)
    {
        var items = await _context.Items
            .AsNoTracking()
            .Where(i =>
                i.Status == ItemStatus.Borrowed ||
                i.Status == ItemStatus.Damaged)
            .OrderBy(i => i.Name)
            .ToListAsync();

        var borrowers = await _context.Borrowers
            .AsNoTracking()
            .Where(b => !b.IsSuspended)
            .OrderBy(b => b.FullName)
            .ToListAsync();

        ViewBag.Items = new SelectList(
            items,
            "Id",
            "Name",
            selectedItemId);

        ViewBag.Borrowers = new SelectList(
            borrowers,
            "Id",
            "FullName",
            selectedBorrowerId);
    }
}
