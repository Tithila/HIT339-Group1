using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers;

[Authorize(Roles = "Manager")]
public class ItemRequestsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ItemRequestsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var requests = await _context.ItemRequests
            .OrderByDescending(r => r.RequestedDate)
            .ToListAsync();

        return View(requests);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? managerNotes)
    {
        var request = await _context.ItemRequests.FindAsync(id);

        if (request == null)
        {
            return NotFound();
        }

        request.Status = "Approved";
        request.ManagerNotes = managerNotes;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? managerNotes)
    {
        var request = await _context.ItemRequests.FindAsync(id);

        if (request == null)
        {
            return NotFound();
        }

        request.Status = "Rejected";
        request.ManagerNotes = managerNotes;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}