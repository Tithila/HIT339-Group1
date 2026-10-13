using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;

    public AuditService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(
        string action,
        string entityName,
        int? entityId,
        string? details,
        string? userName = null)
    {
        var log = new AuditLog
        {
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
            UserName = userName,
            Timestamp = DateTime.Now
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}