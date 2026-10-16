using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryManagementSystem.Services
{
    public class ScheduledReportService : BackgroundService
    {
        private readonly ILogger<ScheduledReportService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public ScheduledReportService(
            ILogger<ScheduledReportService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "ScheduledReportService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow;

                    // Monthly manager report
                    if (now.Day == 1)
                    {
                        using var scope =
                            _scopeFactory.CreateScope();

                        var reporting =
                            scope.ServiceProvider
                                .GetRequiredService<IReportingService>();

                        await reporting.ExportPdfAsync(
                            "MonthlyManagerReport");

                        _logger.LogInformation(
                            "Monthly report generation triggered.");
                    }

                    // Weekly digest placeholder
                    if (now.DayOfWeek == DayOfWeek.Monday)
                    {
                        _logger.LogInformation(
                            "Weekly digest generation placeholder.");
                    }
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    // Normal application shutdown.
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "ScheduledReportService error");
                }

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

            _logger.LogInformation(
                "ScheduledReportService stopped.");
        }
    }
}