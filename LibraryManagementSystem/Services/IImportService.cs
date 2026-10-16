using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using LibraryManagementSystem.ViewModels;

namespace LibraryManagementSystem.Services
{
    public interface IImportService
    {
        Task<ImportPreviewViewModel> PreviewImportAsync(IFormFile file, long maxFileSizeBytes = 5_242_880); // default ~5MB
        Task<ImportImportResult> ConfirmImportAsync(ImportPreviewViewModel preview, bool anonymize = false);
    }

    public class ImportImportResult
    {
        public int ImportedCount { get; set; }
        public int FailedCount { get; set; }
        public string? SummaryMessage { get; set; }
    }
}
