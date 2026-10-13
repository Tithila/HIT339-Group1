namespace LibraryManagementSystem.Services;

public interface IAuditService
{
    Task LogAsync(
        string action,
        string entityName,
        int? entityId,
        string? details,
        string? userName = null);
}