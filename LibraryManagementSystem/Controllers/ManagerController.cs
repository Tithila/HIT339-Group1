using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace LibraryManagementSystem.Controllers;

[Authorize(Roles = "Manager")]
public class ManagerController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IReportingService _reporting;

    public ManagerController(ApplicationDbContext context, IReportingService reporting)
    {
        _context = context;
        _reporting = reporting;
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

        // Populate analytics using ReportingService
        model.MonthlyLoans = await _reporting.GetMonthlyLoansAsync(12);
        model.LoansByBranch = await _reporting.GetLoansByBranchAsync();
        model.InventoryHealth = await _reporting.GetInventoryHealthAsync();
        model.ReservationDemand = await _reporting.GetReservationDemandAsync(10);

        return View(model);
    }

    // CSV export endpoints
    [HttpGet]
    public async Task<IActionResult> ExportItemsCsv(bool anonymize = false)
    {
        var csv = await _reporting.ExportCsvAsync("Items", anonymize);
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", "items_export.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportAuditCsv()
    {
        var csv = await _reporting.ExportCsvAsync("AuditLog");
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", "audit_export.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportReportPdf(string reportName = "MonthlyManagerReport", bool anonymize = false)
    {
        var pdf = await _reporting.ExportPdfAsync(reportName, anonymize);
        return File(pdf, "application/pdf", $"{reportName}.pdf");
    }
}
