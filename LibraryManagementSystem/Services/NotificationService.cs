
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services;

public class NotificationService(
    ApplicationDbContext db,
    ILogger<NotificationService> logger)
    : INotificationService
{
    public async Task<NotificationLog> SendAsync(
        int? borrowerId,
        string recipient,
        NotificationChannel channel,
        NotificationType type,
        string message)
    {
        if (string.IsNullOrWhiteSpace(recipient))
            throw new ArgumentException(
                "A notification recipient is required.");

        var notification = new NotificationLog
        {
            BorrowerId = borrowerId,
            Recipient = recipient,
            Channel = channel,
            Type = type,
            Message = message,
            SentDate = DateTime.UtcNow,
            Status = NotificationStatus.Simulated
        };

        db.NotificationLogs.Add(notification);
        await db.SaveChangesAsync();

        logger.LogInformation(
            "Simulated {Channel} notification #{Id}, type {Type}",
            channel, notification.Id, type);

        return notification;
    }

    public async Task<int> SendDueSoonRemindersAsync()
    {
        var today = DateTime.Today;

        var loans = await db.Loans
            .Include(l => l.Borrower)
            .Include(l => l.Item)
            .Where(l => l.ReturnDate == null &&
                        l.DueDate >= today &&
                        l.DueDate < today.AddDays(4))
            .ToListAsync();

        int count = 0;

        foreach (var loan in loans)
        {
            if (loan.Borrower == null)
                continue;

            int days = (loan.DueDate.Date - today).Days;

            if (days != 1 && days != 3)
                continue;

            var exists = await db.NotificationLogs.AnyAsync(n =>
                n.BorrowerId == loan.BorrowerId &&
                n.Type == NotificationType.DueSoon &&
                n.Message.Contains(
                    $"Loan #{loan.Id} due reminder") &&
                n.SentDate.Date == DateTime.UtcNow.Date);

            if (exists)
                continue;

            await SendAsync(
                loan.BorrowerId,
                loan.Borrower.Email,
                NotificationChannel.Email,
                NotificationType.DueSoon,
                $"Loan #{loan.Id} due reminder: " +
                $"{loan.Item?.Name ?? "Library item"} " +
                $"is due in {days} day(s) on " +
                $"{loan.DueDate:dd MMM yyyy}.");

            count++;
        }

        return count;
    }

    public async Task<int> SendOverdueNotificationsAsync()
    {
        var today = DateTime.Today;

        var loans = await db.Loans
            .Include(l => l.Borrower)
            .Include(l => l.Item)
            .Where(l => l.ReturnDate == null &&
                        l.DueDate < today)
            .ToListAsync();

        int count = 0;

        foreach (var loan in loans)
        {
            if (loan.Borrower == null)
                continue;

            var exists = await db.NotificationLogs.AnyAsync(n =>
                n.BorrowerId == loan.BorrowerId &&
                n.Type == NotificationType.Overdue &&
                n.Message.Contains(
                    $"Loan #{loan.Id} overdue") &&
                n.SentDate.Date == DateTime.UtcNow.Date);

            if (exists)
                continue;

            await SendAsync(
                loan.BorrowerId,
                loan.Borrower.Email,
                NotificationChannel.Email,
                NotificationType.Overdue,
                $"Loan #{loan.Id} overdue: " +
                $"{loan.Item?.Name ?? "Library item"} " +
                $"was due on {loan.DueDate:dd MMM yyyy}. " +
                $"Current fine: ${loan.FineAmount:F2}.");

            count++;
        }

        return count;
    }
}
