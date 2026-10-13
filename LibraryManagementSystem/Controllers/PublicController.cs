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

    // GET: Public/Search
    public async Task<IActionResult> Search(
        string? searchTerm,
        string? itemType,
        ItemStatus? status)
    {
        var query = _context.Items.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(i =>
                i.Name.Contains(searchTerm) ||
                i.LibraryCode.Contains(searchTerm) ||
                i.Description.Contains(searchTerm));
        }

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

        if (status.HasValue)
        {
            query = query.Where(i => i.Status == status.Value);
        }

        var model = new PublicSearchViewModel
        {
            SearchTerm = searchTerm,
            ItemType = itemType,
            Status = status,
            Results = await query
                .OrderBy(i => i.Name)
                .ToListAsync()
        };

        return View(model);
    }

    // GET: Public/RequestItem
    public IActionResult RequestItem()
    {
        return View(new ItemRequest());
    }

    // POST: Public/RequestItem
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestItem(ItemRequest itemRequest)
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

        return RedirectToAction(nameof(RequestItem));
    }
}