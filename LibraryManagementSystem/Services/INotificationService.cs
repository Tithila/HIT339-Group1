
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Services;

public interface INotificationService
{
    Task<NotificationLog> SendAsync(
        int? borrowerId,
        string recipient,
        NotificationChannel channel,
        NotificationType type,
        string message);

    Task<int> SendDueSoonRemindersAsync();

    Task<int> SendOverdueNotificationsAsync();
}
