
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LibraryManagementSystem.Controllers;

[Authorize(Roles = "Admin")]
public class TransfersController : Controller
{
    private readonly ITransferService _transferService;
    private readonly ApplicationDbContext _context;

    public TransfersController(
        ITransferService transferService,
        ApplicationDbContext context)
    {
        _transferService = transferService;
        _context = context;
    }

    // GET: Transfers
    public async Task<IActionResult> Index()
    {
        var transfers = await _transferService.GetAllAsync();

        return View(transfers);
    }

    // GET: Transfers/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var transfer = await _transferService.GetByIdAsync(id);

        if (transfer == null)
            return NotFound();

        return View(transfer);
    }

    // GET: Transfers/Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadDropdownsAsync();

        return View();
    }

    // POST: Transfers/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        int itemId,
        int toBranchId)
    {
        if (itemId <= 0 || toBranchId <= 0)
        {
            ModelState.AddModelError(
                "",
                "Please select an item and a destination branch.");

            await LoadDropdownsAsync(itemId, toBranchId);
            return View();
        }

        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        try
        {
            await _transferService.RequestTransferAsync(
                itemId,
                toBranchId,
                userId);

            TempData["SuccessMessage"] =
                "Transfer request created successfully.";

            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);

            await LoadDropdownsAsync(itemId, toBranchId);
            return View();
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError("", ex.Message);

            await LoadDropdownsAsync(itemId, toBranchId);
            return View();
        }
    }

    // POST: Transfers/Approve/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        try
        {
            await _transferService.ApproveTransferAsync(
                id,
                CurrentUserName());

            TempData["SuccessMessage"] =
                "Transfer approved successfully.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: Transfers/Reject/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        try
        {
            await _transferService.RejectTransferAsync(
                id,
                CurrentUserName());

            TempData["SuccessMessage"] =
                "Transfer rejected successfully.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: Transfers/MarkInTransit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkInTransit(int id)
    {
        try
        {
            await _transferService.MarkInTransitAsync(
                id,
                CurrentUserName());

            TempData["SuccessMessage"] =
                "Transfer marked as in transit.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: Transfers/Complete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        try
        {
            await _transferService.CompleteTransferAsync(
                id,
                CurrentUserName());

            TempData["SuccessMessage"] =
                "Transfer completed successfully.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // Load only eligible items and available library branches.
    private async Task LoadDropdownsAsync(
        int? selectedItemId = null,
        int? selectedBranchId = null)
    {
        // Exclude items with an active transfer request.
        // Completed and rejected transfers do not block new requests.
        var items = await _context.Items
            .AsNoTracking()
            .Where(i =>
                i.BranchId != null &&
                i.Status == ItemStatus.Available &&
                !_context.TransferRequests.Any(t =>
                    t.ItemId == i.Id &&
                    (t.Status == TransferStatus.Requested ||
                     t.Status == TransferStatus.Approved ||
                     t.Status == TransferStatus.InTransit)))
            .OrderBy(i => i.Name)
            .Select(i => new
            {
                i.Id,
                DisplayName =
                    i.Name + " (" + i.LibraryCode + ")"
            })
            .ToListAsync();

        var branches = await _context.Branches
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .ToListAsync();

        ViewBag.Items = new SelectList(
            items,
            "Id",
            "DisplayName",
            selectedItemId);

        ViewBag.Branches = new SelectList(
            branches,
            "Id",
            "Name",
            selectedBranchId);
    }

    private string CurrentUserName()
    {
        return User.Identity?.Name ?? "Unknown";
    }
}
