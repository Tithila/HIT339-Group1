
using System;
using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

public enum NotificationChannel
{
    Email,
    SMS
}

public enum NotificationType
{
    Borrowed,
    DueSoon,
    Overdue,
    Fine,
    ReservationReady,
    Welcome

}

public enum NotificationStatus
{
    Simulated,
    Failed
}

public class NotificationLog
{
    public int Id { get; set; }

    public int? BorrowerId { get; set; }

    public Borrower? Borrower { get; set; }

    [Required]
    [StringLength(150)]
    public string Recipient { get; set; } = string.Empty;

    public NotificationChannel Channel { get; set; }

    public NotificationType Type { get; set; }

    [Required]
    [StringLength(2000)]
    public string Message { get; set; } = string.Empty;

    public DateTime SentDate { get; set; } = DateTime.UtcNow;

    public NotificationStatus Status { get; set; }
        = NotificationStatus.Simulated;
}


