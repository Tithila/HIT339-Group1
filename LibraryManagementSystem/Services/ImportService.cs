using LibraryManagementSystem.Data;
using LibraryManagementSystem.DTOs;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using LibraryManagementSystem.Services.CsvMaps;

namespace LibraryManagementSystem.Services
{
    public class ImportService : IImportService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAuditService _audit;

        private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "Book",
            "Music",
            "Toy"
        };

        public ImportService(ApplicationDbContext db, IAuditService audit)
        {
            _db = db;
            _audit = audit;
        }

        public async Task<ImportPreviewViewModel> PreviewImportAsync(IFormFile file, long maxFileSizeBytes = 5_242_880)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));

            // Basic file checks
            if (file.Length == 0) throw new InvalidOperationException("Empty file.");
            if (file.Length > maxFileSizeBytes) throw new InvalidOperationException("File too large.");
            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) && !file.ContentType.Contains("csv"))
            {
                throw new InvalidOperationException("Unsupported file type. Please upload a CSV file.");
            }

            var preview = new ImportPreviewViewModel();

            var fileCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);

            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header?.Trim() ?? string.Empty,
                MissingFieldFound = null,
                BadDataFound = null,
                HeaderValidated = null,
                TrimOptions = TrimOptions.Trim
            };

            using var csv = new CsvReader(reader, csvConfig);
            csv.Context.RegisterClassMap<ImportItemDtoMap>();

            if (!await csv.ReadAsync() || !csv.ReadHeader())
            {
                throw new InvalidOperationException("CSV contains no header or data.");
            }

            while (await csv.ReadAsync())
            {
                var rowNum = csv.Context.Parser?.Row ?? 0;

                ImportItemDto? dto = null;
                try
                {
                    dto = csv.GetRecord<ImportItemDto>();
                }
                catch (Exception ex)
                {
                    var errRow = new ImportPreviewRow { RowNumber = rowNum, Item = new ImportItemDto { RowNumber = rowNum } };
                    errRow.Errors.Add($"Parse error: {ex.Message}");
                    errRow.IsValid = false;
                    preview.InvalidRows.Add(errRow);
                    continue;
                }

                dto.RowNumber = rowNum;
                var row = new ImportPreviewRow { RowNumber = rowNum, Item = dto };

                // Validate required fields and item type
                if (string.IsNullOrWhiteSpace(dto.ItemType)) row.Errors.Add("ItemType is required.");
                else if (!AllowedTypes.Contains(dto.ItemType)) row.Errors.Add($"Unsupported ItemType '{dto.ItemType}'.");

                if (string.IsNullOrWhiteSpace(dto.Name)) row.Errors.Add("Name is required.");
                if (string.IsNullOrWhiteSpace(dto.LibraryCode)) row.Errors.Add("LibraryCode is required.");
                if (string.IsNullOrWhiteSpace(dto.BranchCode)) row.Errors.Add("BranchCode is required.");

                // Check duplicates within file and existing DB
                if (!string.IsNullOrWhiteSpace(dto.LibraryCode))
                {
                    if (!fileCodes.Add(dto.LibraryCode))
                    {
                        row.IsDuplicate = true;
                        row.Errors.Add("Duplicate LibraryCode in file.");
                    }
                    else
                    {
                        bool existsInDb = await _db.Items.AnyAsync(i => i.LibraryCode == dto.LibraryCode);
                        if (existsInDb)
                        {
                            row.IsDuplicate = true;
                            row.Errors.Add("LibraryCode already exists in system.");
                        }
                    }
                }

                // Branch existence
                if (!string.IsNullOrWhiteSpace(dto.BranchCode))
                {
                    var branchExists = await _db.Branches.AnyAsync(b => b.Code == dto.BranchCode);
                    if (!branchExists)
                    {
                        row.Errors.Add($"Branch code '{dto.BranchCode}' not found.");
                    }
                }

                if (row.Errors.Any() || row.IsDuplicate)
                {
                    row.IsValid = false;
                    preview.InvalidRows.Add(row);
                    if (row.IsDuplicate) preview.Duplicates.Add(row);
                }
                else
                {
                    row.IsValid = true;
                    preview.ValidRows.Add(row);
                }
            }

            return preview;
        }

        public async Task<ImportImportResult> ConfirmImportAsync(ImportPreviewViewModel preview, bool anonymize = false)
        {
            if (preview == null) throw new ArgumentNullException(nameof(preview));

            int imported = 0;
            int failed = 0;

            foreach (var r in preview.ValidRows)
            {
                try
                {
                    var dto = r.Item!;
                    // Map DTO to EF entities
                    Item item = dto.ItemType?.Equals("Book", StringComparison.OrdinalIgnoreCase) == true
                        ? new Book()
                        : dto.ItemType?.Equals("Music", StringComparison.OrdinalIgnoreCase) == true
                            ? new Music()
                            : new Toy();

                    item.Name = dto.Name ?? string.Empty;
                    item.Description = dto.Description ?? string.Empty;
                    item.LibraryCode = dto.LibraryCode ?? string.Empty;
                    item.Status = Enum.TryParse<ItemStatus>(dto.Status ?? "Available", true, out var st) ? st : ItemStatus.Available;

                    // Branch lookup
                    var branch = await _db.Branches.FirstOrDefaultAsync(b => b.Code == dto.BranchCode);
                    item.BranchId = branch?.Id;

                    if (item is Book book)
                    {
                        book.Author = dto.Author ?? string.Empty;
                        book.Genre = dto.Genre ?? string.Empty;
                    }
                    else if (item is Music music)
                    {
                        music.Artist = dto.Artist ?? string.Empty;
                        music.Year = dto.Year ?? 0;
                    }
                    else if (item is Toy toy)
                    {
                        toy.Type = dto.Type ?? string.Empty;
                        toy.RecommendedAge = dto.RecommendedAge ?? 0;
                    }

                    _db.Items.Add(item);
                    imported++;
                }
                catch
                {
                    failed++;
                }
            }

            await _db.SaveChangesAsync();

            // Audit log
            var summary = $"Imported {imported} items, failed {failed}, from CSV import.";
            await _audit.LogAsync("Import", "ItemImport", null, summary, null);

            return new ImportImportResult { ImportedCount = imported, FailedCount = failed, SummaryMessage = summary };
        }

        private static string[] SplitCsvLine(string line, int expectedColumns)
        {
            // Basic CSV parser supporting quoted fields and escaped quotes.
            var cols = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    // Handle escaped quote
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++; // skip next quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    cols.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            cols.Add(current.ToString().Trim());

            // pad to expected columns
            while (cols.Count < expectedColumns)
            {
                cols.Add(string.Empty);
            }

            return cols.ToArray();
        }


    }
}
