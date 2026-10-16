using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryManagementSystem.Services
{
    public class ScheduledReportService : BackgroundService
    {
        private readonly ILogger<ScheduledReportService> _logger;
        private readonly IServiceProvider _services;

        public ScheduledReportService(ILogger<ScheduledReportService> logger, IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ScheduledReportService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow;
                    if (now.Day == 1)
                    {
                        using var scope = _services.CreateScope();
                        var reporting = scope.ServiceProvider.GetRequiredService<IReportingService>();
                        await reporting.ExportPdfAsync("MonthlyManagerReport");
                        _logger.LogInformation("Monthly report generation triggered.");
                    }

                    if (now.DayOfWeek == DayOfWeek.Monday)
                    {
                        _logger.LogInformation("Weekly digest generation placeholder.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ScheduledReportService error");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
