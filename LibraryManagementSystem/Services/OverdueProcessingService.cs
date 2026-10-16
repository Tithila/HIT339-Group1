
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessLoansAsync(stoppingToken);
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
                    "Automatic background processing failed.");
            }

            // Run again approximately every 24 hours.
            try
            {
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
    }

    private async Task ProcessLoansAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var fineCalculator = scope.ServiceProvider
            .GetRequiredService<IFineCalculator>();

        var borrowerService = scope.ServiceProvider
            .GetRequiredService<IBorrowerEligibilityService>();

        var auditService = scope.ServiceProvider
            .GetRequiredService<IAuditService>();

        var notificationService = scope.ServiceProvider
            .GetRequiredService<INotificationService>();

        // NEW: Resolve the existing reservation service.
        var reservationService = scope.ServiceProvider
            .GetRequiredService<IReservationService>();

        // NEW: Automatically expire uncollected reservations.
        // Run this independently of loan processing so that
        // an expiry error does not stop overdue checks.
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            int expiredCount =
                await reservationService.ProcessExpiredReservationsAsync();

            _logger.LogInformation(
                "Automatic reservation expiry completed. " +
                "Expired reservations: {Count}",
                expiredCount);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Automatic reservation expiry processing failed.");
        }

        var today = DateTime.Today;

        // Retrieve all active loans.
        var activeLoans = await context.Loans
            .Include(l => l.Item)
            .Include(l => l.Borrower)
            .Where(l => l.ReturnDate == null)
            .ToListAsync(cancellationToken);

        foreach (var loan in activeLoans)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                int daysUntilDue =
                    (loan.DueDate.Date - today).Days;

                // Preserve existing overdue fine calculations.
                if (daysUntilDue < 0)
                {
                    loan.IsOverdue = true;

                    // Temporarily set a return date for
                    // the existing fine calculator.
                    loan.ReturnDate = DateTime.Now;

                    try
                    {
                        loan.FineAmount =
                            await fineCalculator.CalculateFineAsync(loan);
                    }
                    finally
                    {
                        // Item has NOT been returned.
                        loan.ReturnDate = null;
                    }

                    await context.SaveChangesAsync(cancellationToken);

                    // Preserve borrower eligibility checks.
                    await borrowerService
                        .CheckAndUpdateSuspensionAsync(
                            loan.BorrowerId);

                    // Preserve audit logging.
                    await auditService.LogAsync(
                        "Automatic Overdue Check",
                        "Loan",
                        loan.Id,
                        $"Loan {loan.Id} processed automatically. " +
                        $"Current fine: ${loan.FineAmount:F2}",
                        "System");
                }

                // Automatic simulated notifications.
                if (loan.Borrower == null ||
                    string.IsNullOrWhiteSpace(loan.Borrower.Email))
                {
                    continue;
                }

                if (daysUntilDue == 2)
                {
                    await SendReminderAsync(
                        context,
                        notificationService,
                        loan,
                        "Due in 2 days",
                        $"Hello {loan.Borrower.FullName}, " +
                        $"your library item '{loan.Item?.Name}' " +
                        $"is due on {loan.DueDate:dd MMM yyyy}. " +
                        "Please return or renew it before the due date.");
                }
                else if (daysUntilDue == 1)
                {
                    await SendReminderAsync(
                        context,
                        notificationService,
                        loan,
                        "Due tomorrow",
                        $"Hello {loan.Borrower.FullName}, " +
                        $"your library item '{loan.Item?.Name}' " +
                        $"is due tomorrow, {loan.DueDate:dd MMM yyyy}.");
                }
                else if (daysUntilDue == -1)
                {
                    await SendReminderAsync(
                        context,
                        notificationService,
                        loan,
                        "Overdue day 1",
                        $"Hello {loan.Borrower.FullName}, " +
                        $"your library item '{loan.Item?.Name}' " +
                        "is overdue. Please return it as soon as possible.");
                }
                else if (daysUntilDue == -7)
                {
                    await SendReminderAsync(
                        context,
                        notificationService,
                        loan,
                        "Overdue day 7",
                        $"Hello {loan.Borrower.FullName}, " +
                        $"your library item '{loan.Item?.Name}' " +
                        "is now seven days overdue. " +
                        "Please contact the library.");
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Automatic processing failed for loan {LoanId}",
                    loan.Id);

                // Continue processing other loans.
            }
        }
    }

    // Prevent duplicate simulated reminders.
    private async Task SendReminderAsync(
        ApplicationDbContext context,
        INotificationService notificationService,
        Loan loan,
        string reminderKind,
        string message)
    {
        string marker =
            $"[Loan:{loan.Id};Reminder:{reminderKind}]";

        var alreadySent = await context.NotificationLogs
            .AnyAsync(n =>
                n.BorrowerId == loan.BorrowerId &&
                n.Message.Contains(marker));

        if (alreadySent)
            return;

        var borrower = loan.Borrower;

        if (borrower == null)
            return;

        var notificationType =
            reminderKind.StartsWith(
                "Overdue",
                StringComparison.Ordinal)
                ? NotificationType.Overdue
                : NotificationType.DueSoon;

        await notificationService.SendAsync(
            borrower.Id,
            borrower.Email,
            NotificationChannel.Email,
            notificationType,
            $"{marker} {message}");

        _logger.LogInformation(
            "Recorded {ReminderKind} notification for loan {LoanId}",
            reminderKind,
            loan.Id);
    }
}
