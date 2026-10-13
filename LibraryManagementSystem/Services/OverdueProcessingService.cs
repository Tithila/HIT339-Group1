using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services;

public class OverdueProcessingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public OverdueProcessingService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();

            var context =
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var fineCalculator =
                scope.ServiceProvider.GetRequiredService<IFineCalculator>();

            var borrowerService =
                scope.ServiceProvider.GetRequiredService<IBorrowerEligibilityService>();

            var auditService =
                scope.ServiceProvider.GetRequiredService<IAuditService>();

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
                loan.ReturnDate = DateTime.Now;

                loan.FineAmount =
                    await fineCalculator.CalculateFineAsync(loan);

                // Keep loan active — item has NOT actually been returned
                loan.ReturnDate = null;

                await context.SaveChangesAsync(stoppingToken);

                await borrowerService
                    .CheckAndUpdateSuspensionAsync(loan.BorrowerId);

                await auditService.LogAsync(
                    "Automatic Overdue Check",
                    "Loan",
                    loan.Id,
                    $"Loan {loan.Id} processed automatically. Current fine: ${loan.FineAmount:F2}",
                    "System");
            }

            // Runs once every 24 hours
            await Task.Delay(
                TimeSpan.FromHours(24),
                stoppingToken);
        }
    }
}