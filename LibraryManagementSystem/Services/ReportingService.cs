using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;

namespace LibraryManagementSystem.Services
{
    public interface IReportingService
    {
        Task<List<MonthlyLoanStat>> GetMonthlyLoansAsync(int months = 12);
        Task<List<BranchLoanStat>> GetLoansByBranchAsync();
        Task<decimal> GetFineRevenueAsync(DateTime from, DateTime to);
        Task<InventoryHealth> GetInventoryHealthAsync();
        Task<List<ReservationDemandStat>> GetReservationDemandAsync(int top = 10);

        // Exports
        Task<string> ExportCsvAsync(string tableName, bool anonymize = false);
        Task<byte[]> ExportPdfAsync(string reportName, bool anonymize = false);
    }

    public class ReportingService : IReportingService
    {
        private readonly ApplicationDbContext _db;

        public ReportingService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<MonthlyLoanStat>> GetMonthlyLoansAsync(int months = 12)
        {
            var from = DateTime.UtcNow.AddMonths(-months + 1);
            var q = await _db.Loans
                .Where(l => l.BorrowDate >= from)
                .GroupBy(l => new { l.BorrowDate.Year, l.BorrowDate.Month })
                .Select(g => new MonthlyLoanStat
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(s => s.Year).ThenBy(s => s.Month)
                .ToListAsync();

            return q;
        }

        public async Task<List<BranchLoanStat>> GetLoansByBranchAsync()
        {
            var q = await _db.Loans
                .Include(l => l.Item!)
                .ThenInclude(i => i.Branch)
                .Where(l => l.Item != null && l.Item.Branch != null)
                .GroupBy(l => new
                {
                    BranchId = l.Item!.BranchId,
                    Code = l.Item.Branch!.Code,
                    Name = l.Item.Branch.Name
                })
                .Select(g => new BranchLoanStat
                {
                    BranchId = g.Key.BranchId ?? 0,
                    BranchCode = g.Key.Code,
                    BranchName = g.Key.Name,
                    Count = g.Count()
                })
                .ToListAsync();

            return q;
        }

        public async Task<decimal> GetFineRevenueAsync(DateTime from, DateTime to)
        {
            var q = await _db.Loans
                .Where(l => l.ReturnDate >= from && l.ReturnDate <= to)
                .SumAsync(l => (decimal?)l.FineAmount) ?? 0m;
            return q;
        }

        public async Task<InventoryHealth> GetInventoryHealthAsync()
        {
            var total = await _db.Items.CountAsync();
            var damaged = await _db.Items.CountAsync(i => i.Status == ItemStatus.Damaged || i.Status == ItemStatus.Destroy);
            return new InventoryHealth { Total = total, DamagedOrDestroyed = damaged };
        }

        public async Task<List<ReservationDemandStat>> GetReservationDemandAsync(int top = 10)
        {
            var q = await _db.Reservations
                .GroupBy(r => r.ItemId)
                .Select(g => new ReservationDemandStat { ItemId = g.Key, Count = g.Count() })
                .OrderByDescending(r => r.Count)
                .Take(top)
                .ToListAsync();
            return q;
        }

        public Task<string> ExportCsvAsync(string tableName, bool anonymize = false)
        {
            // Very small helper to export CSV as string. Implemented for Items and AuditLog as examples.
            if (tableName.Equals("AuditLog", StringComparison.OrdinalIgnoreCase))
            {
                var rows = _db.AuditLogs.Select(a => new { a.Timestamp, a.UserName, a.Action, a.Details });
                var csv = "Timestamp,User,Action,Details\n" + string.Join('\n', rows.AsEnumerable().Select(r => $"\"{r.Timestamp}\",\"{r.UserName}\",\"{r.Action}\",\"{r.Details}\""));
                return Task.FromResult(csv);
            }

            if (tableName.Equals("Items", StringComparison.OrdinalIgnoreCase))
            {
                var rows = _db.Items.Include(i => i.Branch).Select(i => new
                {
                    i.LibraryCode,
                    i.Name,
                    i.Description,
                    BranchCode = i.Branch != null ? i.Branch.Code : string.Empty,
                    Status = i.Status.ToString()
                });

                var header = "LibraryCode,Name,Description,BranchCode,Status";
                var csv = header + "\n" + string.Join('\n', rows.AsEnumerable().Select(r => $"\"{r.LibraryCode}\",\"{r.Name}\",\"{r.Description}\",\"{r.BranchCode}\",\"{r.Status}\"") );
                return Task.FromResult(csv);
            }

            return Task.FromResult(string.Empty);
        }

        public async Task<byte[]> ExportPdfAsync(string reportName, bool anonymize = false)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            try
            {
                var pdfBytes = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(20);

                        page.Header().Text(text =>
                        {
                            text.Span($"Library Management System - {reportName}").FontSize(18).Bold();
                        });

                        page.Content().Column(column =>
                        {
                            column.Spacing(8);

                            column.Item().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC").FontSize(9).SemiBold();

                            if (reportName.Equals("MonthlyLoans", StringComparison.OrdinalIgnoreCase))
                                RenderMonthlyLoansReport(column);
                            else if (reportName.Equals("BranchLoans", StringComparison.OrdinalIgnoreCase))
                                RenderBranchLoansReport(column);
                            else if (reportName.Equals("InventoryHealth", StringComparison.OrdinalIgnoreCase))
                                RenderInventoryHealthReport(column);
                            else if (reportName.Equals("ReservationDemand", StringComparison.OrdinalIgnoreCase))
                                RenderReservationDemandReport(column);
                            else
                                column.Item().Text("Unknown report type").FontSize(12);
                        });

                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("Page ").FontSize(9);
                            x.CurrentPageNumber();
                        });
                    });
                }).GeneratePdf();

                return await Task.FromResult(pdfBytes);
            }
            catch
            {
                return await Task.FromResult(Array.Empty<byte>());
            }
        }

        private void RenderMonthlyLoansReport(ColumnDescriptor column)
        {
            var loans = _db.Loans
                .Where(l => l.BorrowDate >= DateTime.UtcNow.AddMonths(-12))
                .GroupBy(l => new { l.BorrowDate.Year, l.BorrowDate.Month })
                .Select(g => new MonthlyLoanStat
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(s => s.Year).ThenBy(s => s.Month)
                .ToList();

            column.Item().Text("Monthly Loans (last 12 months)").FontSize(13).Bold();
            column.Item().PaddingTop(4);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });

                table.Header(header =>
                {
                    header.Cell().Text("Year").Bold();
                    header.Cell().Text("Month").Bold();
                    header.Cell().Text("Loans").Bold();
                });

                foreach (var s in loans)
                {
                    table.Cell().Text(s.Year.ToString());
                    table.Cell().Text(s.Month.ToString());
                    table.Cell().Text(s.Count.ToString());
                }
            });
        }

        private void RenderBranchLoansReport(ColumnDescriptor column)
        {
            var branchLoans = _db.Loans
                .Include(l => l.Item!)
                .ThenInclude(i => i.Branch)
                .Where(l => l.Item != null && l.Item.Branch != null)
                .GroupBy(l => new
                {
                    BranchId = l.Item!.BranchId,
                    Code = l.Item.Branch!.Code,
                    Name = l.Item.Branch.Name
                })
                .Select(g => new BranchLoanStat
                {
                    BranchId = g.Key.BranchId ?? 0,
                    BranchCode = g.Key.Code,
                    BranchName = g.Key.Name,
                    Count = g.Count()
                })
                .OrderByDescending(s => s.Count)
                .ToList();

            column.Item().Text("Loans by Branch").FontSize(13).Bold();
            column.Item().PaddingTop(4);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });

                table.Header(header =>
                {
                    header.Cell().Text("Branch").Bold();
                    header.Cell().Text("Code").Bold();
                    header.Cell().Text("Loans").Bold();
                });

                foreach (var s in branchLoans)
                {
                    table.Cell().Text(s.BranchName ?? "");
                    table.Cell().Text(s.BranchCode ?? "");
                    table.Cell().Text(s.Count.ToString());
                }
            });
        }

        private void RenderInventoryHealthReport(ColumnDescriptor column)
        {
            var health = new InventoryHealth
            {
                Total = _db.Items.Count(),
                DamagedOrDestroyed = _db.Items.Count(i => i.Status == ItemStatus.Damaged || i.Status == ItemStatus.Destroy)
            };

            column.Item().Text("Inventory Health").FontSize(13).Bold();
            column.Item().PaddingTop(4);

            column.Item().Text($"Total items: {health.Total}");
            column.Item().Text($"Damaged/Destroyed: {health.DamagedOrDestroyed}");
            column.Item().Text($"Healthy: {health.Total - health.DamagedOrDestroyed}");
        }

        private void RenderReservationDemandReport(ColumnDescriptor column)
        {
            var reservations = _db.Reservations
                .GroupBy(r => r.ItemId)
                .Select(g => new ReservationDemandStat { ItemId = g.Key, Count = g.Count() })
                .OrderByDescending(r => r.Count)
                .Take(10)
                .ToList();

            column.Item().Text("Top Reservations").FontSize(13).Bold();
            column.Item().PaddingTop(4);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });

                table.Header(header =>
                {
                    header.Cell().Text("ItemId").Bold();
                    header.Cell().Text("Reservations").Bold();
                });

                foreach (var r in reservations)
                {
                    table.Cell().Text(r.ItemId?.ToString() ?? "");
                    table.Cell().Text(r.Count.ToString());
                }
            });
        }
    }

    // Reporting models
    public class MonthlyLoanStat { public int Year { get; set; } public int Month { get; set; } public int Count { get; set; } }
    public class BranchLoanStat { public int BranchId { get; set; } public string? BranchCode { get; set; } public string? BranchName { get; set; } public int Count { get; set; } }
    public class InventoryHealth { public int Total { get; set; } public int DamagedOrDestroyed { get; set; } }
    public class ReservationDemandStat { public int? ItemId { get; set; } public int Count { get; set; } }
}
