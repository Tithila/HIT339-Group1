using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers;

public class PublicController : Controller
{
    private readonly ApplicationDbContext _context;

    public PublicController(ApplicationDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // PUBLIC LIBRARY SEARCH
    // =========================================================

    // GET: Public/Search
    public async Task<IActionResult> Search(
        string? searchTerm,
        string? itemType,
        ItemStatus? status,
        int? branchId,
        bool searched = false)
    {
        var branches = await _context.Branches
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .ToListAsync();

        // Initial page load:
        // do NOT display the entire catalogue yet.
        if (!searched)
        {
            var initialModel = new PublicSearchViewModel
            {
                SearchTerm = searchTerm,
                ItemType = itemType,
                Status = status,
                BranchId = branchId,
                Branches = branches,
                Results = new List<Item>(),
                HasSearched = false
            };

            return View(initialModel);
        }

        var query = _context.Items
            .Include(i => i.Branch)
            .AsNoTracking()
            .AsQueryable();

        // Search by item name, code or description.
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.Trim();

            query = query.Where(i =>
                i.Name.Contains(searchTerm) ||
                i.LibraryCode.Contains(searchTerm) ||
                i.Description.Contains(searchTerm));
        }

        // Filter by item type.
        if (!string.IsNullOrWhiteSpace(itemType))
        {
            query = itemType switch
            {
                "Book" => query.Where(i => i is Book),
                "Music" => query.Where(i => i is Music),
                "Toy" => query.Where(i => i is Toy),
                _ => query
            };
        }

        // Filter by status.
        if (status.HasValue)
        {
            query = query.Where(
                i => i.Status == status.Value);
        }

        // Filter by physical branch.
        if (branchId.HasValue)
        {
            query = query.Where(
                i => i.BranchId == branchId.Value);
        }

        var results = await query
            .OrderBy(i => i.Name)
            .ToListAsync();

        var model = new PublicSearchViewModel
        {
            SearchTerm = searchTerm,
            ItemType = itemType,
            Status = status,
            BranchId = branchId,
            Branches = branches,
            Results = results,
            HasSearched = true
        };

        return View(model);
    }

    // =========================================================
    // PUBLIC ITEM REQUEST
    // =========================================================

    // GET: Public/RequestItem
    public IActionResult RequestItem()
    {
        return View(new ItemRequest());
    }

    // POST: Public/RequestItem
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestItem(
        ItemRequest itemRequest)
    {
        if (!ModelState.IsValid)
        {
            return View(itemRequest);
        }

        itemRequest.Status = "Pending";
        itemRequest.RequestedDate = DateTime.Now;

        _context.ItemRequests.Add(itemRequest);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Your item request has been submitted successfully.";

        return RedirectToAction(
            nameof(RequestItem));
    }
}