using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

public class AuditLog
{
    public int Id { get; set; }

    [Required]
    public string Action { get; set; } = string.Empty;

    [Required]
    public string EntityName { get; set; } = string.Empty;

    public int? EntityId { get; set; }

    public string? Details { get; set; }

    public string? UserName { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.Now;
}