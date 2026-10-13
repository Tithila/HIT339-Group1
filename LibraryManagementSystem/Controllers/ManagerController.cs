using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace LibraryManagementSystem.Controllers;

[Authorize(Roles = "Manager")]
public class ManagerController : Controller
{
    private readonly ApplicationDbContext _context;

    public ManagerController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var model = new ManagerDashboardViewModel
        {
            TotalItems = await _context.Items.CountAsync(),

            AvailableItems = await _context.Items
                .CountAsync(i => i.Status == ItemStatus.Available),

            BorrowedItems = await _context.Items
                .CountAsync(i => i.Status == ItemStatus.Borrowed),

            DamagedItems = await _context.Items
                .CountAsync(i => i.Status == ItemStatus.Damaged),

            DestroyedItems = await _context.Items
                .CountAsync(i => i.Status == ItemStatus.Destroy),

            ActiveLoans = await _context.Loans
                .CountAsync(l => l.ReturnDate == null),

            OverdueLoans = await _context.Loans
                .CountAsync(l => l.IsOverdue && l.ReturnDate == null),

            ReturnedLoans = await _context.Loans
                .CountAsync(l => l.ReturnDate != null),

            TotalFinesGenerated = await _context.Loans
                .SumAsync(l => l.FineAmount),

            TotalFinesPaid = await _context.Loans
                .Where(l => l.FinePaid)
                .SumAsync(l => l.FineAmount),

            OutstandingFines = await _context.Loans
                .Where(l => !l.FinePaid)
                .SumAsync(l => l.FineAmount),

            SuspendedBorrowers = await _context.Borrowers
                .CountAsync(b => b.IsSuspended),

            TotalRepairCost = await _context.ReturnInspections
                .SumAsync(r => r.RepairCost),

            TotalReplacementCost = await _context.ReturnInspections
                .SumAsync(r => r.ReplacementCost)
        };

        return View(model);
    }
}