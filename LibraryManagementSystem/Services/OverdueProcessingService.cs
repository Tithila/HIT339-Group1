using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services;

public class OverdueProcessingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OverdueProcessingService> _logger;

    public OverdueProcessingService(
        IServiceScopeFactory scopeFactory,
        ILogger<OverdueProcessingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "OverdueProcessingService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var context =
                    scope.ServiceProvider
                        .GetRequiredService<ApplicationDbContext>();

                var fineCalculator =
                    scope.ServiceProvider
                        .GetRequiredService<IFineCalculator>();

                var borrowerService =
                    scope.ServiceProvider
                        .GetRequiredService<IBorrowerEligibilityService>();

                var auditService =
                    scope.ServiceProvider
                        .GetRequiredService<IAuditService>();

                var notificationService =
                    scope.ServiceProvider
                        .GetRequiredService<INotificationService>();

                // =====================================================
                // PROCESS OVERDUE LOANS
                // =====================================================

                var overdueLoans = await context.Loans
                    .Include(l => l.Item)
                    .Where(l =>
                        l.ReturnDate == null &&
                        l.DueDate.Date < DateTime.Today)
                    .ToListAsync(stoppingToken);

                foreach (var loan in overdueLoans)
                {
                    loan.IsOverdue = true;

                    // Temporary date so FineCalculator can calculate
                    // the current overdue fine.
                    loan.ReturnDate = DateTime.Now;

                    loan.FineAmount =
                        await fineCalculator
                            .CalculateFineAsync(loan);

                    // Keep the loan active because the item
                    // has not actually been returned.
                    loan.ReturnDate = null;

                    await context.SaveChangesAsync(
                        stoppingToken);

                    await borrowerService
                        .CheckAndUpdateSuspensionAsync(
                            loan.BorrowerId);

                    await auditService.LogAsync(
                        "Automatic Overdue Check",
                        "Loan",
                        loan.Id,
                        $"Loan {loan.Id} processed automatically. " +
                        $"Current fine: ${loan.FineAmount:F2}",
                        "System");
                }

                // =====================================================
                // AUTOMATED NOTIFICATIONS
                // =====================================================

                var dueSoonCount =
                    await notificationService
                        .SendDueSoonRemindersAsync();

                var overdueNotificationCount =
                    await notificationService
                        .SendOverdueNotificationsAsync();

                _logger.LogInformation(
                    "Daily overdue processing complete. " +
                    "{OverdueLoans} overdue loan(s), " +
                    "{DueSoonNotifications} due-soon reminder(s), " +
                    "{OverdueNotifications} overdue notification(s).",
                    overdueLoans.Count,
                    dueSoonCount,
                    overdueNotificationCount);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error during automatic overdue processing.");
            }

            try
            {
                // Run once every 24 hours.
                await Task.Delay(
                    TimeSpan.FromHours(24),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation(
            "OverdueProcessingService stopped.");
    }
}