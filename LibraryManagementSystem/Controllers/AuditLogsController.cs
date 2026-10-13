using LibraryManagementSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace LibraryManagementSystem.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class AuditLogsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AuditLogsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var logs = await _context.AuditLogs
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();

        return View(logs);
    }
}