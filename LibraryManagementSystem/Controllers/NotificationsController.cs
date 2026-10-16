using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Reception,Admin,Manager")]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Notifications
        public async Task<IActionResult> Index(
            string? search,
            NotificationType? type)
        {
            var query = _context.NotificationLogs
                .Include(n => n.Borrower)
                .AsNoTracking()
                .AsQueryable();

            // Search by borrower name or recipient
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(n =>
                    n.Recipient.Contains(search) ||
                    (n.Borrower != null &&
                     n.Borrower.FullName.Contains(search)));
            }

            // Filter by notification type
            if (type.HasValue)
            {
                query = query.Where(n => n.Type == type.Value);
            }

            var notifications = await query
                .OrderByDescending(n => n.SentDate)
                .ThenByDescending(n => n.Id)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.SelectedType = type;

            return View(notifications);
        }
    }
}