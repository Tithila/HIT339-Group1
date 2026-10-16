using LibraryManagementSystem.Services;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

namespace LibraryManagementSystem.Pages.Admin;

[Authorize(Roles = "Admin")]
public class ImportModel : PageModel
{
    private static readonly TimeSpan PreviewLifetime = TimeSpan.FromMinutes(20);
    private readonly IImportService _importService;
    private readonly IMemoryCache _cache;

    public ImportModel(IImportService importService, IMemoryCache cache)
    {
        _importService = importService;
        _cache = cache;
    }

    [BindProperty]
    public IFormFile? UploadFile { get; set; }

    public ImportPreviewViewModel? Preview { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostPreviewAsync()
    {
        if (UploadFile == null)
        {
            ModelState.AddModelError(string.Empty, "Please select a CSV file to upload.");
            return Page();
        }

        try
        {
            Preview = await _importService.PreviewImportAsync(UploadFile);
            var previewId = Guid.NewGuid().ToString("N");
            _cache.Set(GetPreviewCacheKey(previewId), Preview, PreviewLifetime);
            TempData["ImportPreviewId"] = previewId;
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostConfirmAsync()
    {
        if (!TempData.TryGetValue("ImportPreviewId", out var obj) || obj == null)
        {
            ModelState.AddModelError(string.Empty, "No preview available to confirm. Please upload and preview again.");
            return Page();
        }

        var previewId = obj.ToString();
        if (string.IsNullOrEmpty(previewId) ||
            !_cache.TryGetValue(GetPreviewCacheKey(previewId), out ImportPreviewViewModel? preview) ||
            preview == null)
        {
            ModelState.AddModelError(string.Empty, "The preview has expired. Please upload and preview the CSV again.");
            return Page();
        }

        _cache.Remove(GetPreviewCacheKey(previewId));
        var result = await _importService.ConfirmImportAsync(preview);
        TempData["ImportResult"] = $"Imported: {result.ImportedCount}, Failed: {result.FailedCount}";

        return RedirectToPage("/Admin/Import");
    }

    public IActionResult OnPostDownloadErrors()
    {
        var previewId = TempData.Peek("ImportPreviewId") as string;
        if (string.IsNullOrEmpty(previewId) ||
            !_cache.TryGetValue(GetPreviewCacheKey(previewId), out ImportPreviewViewModel? preview) ||
            preview == null)
        {
            TempData["ImportError"] = "The preview has expired. Please upload and preview the CSV again.";
            return RedirectToPage();
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("RowNumber,LibraryCode,Name,Errors");
        foreach (var r in preview.InvalidRows)
        {
            var errors = string.Join(" | ", r.Errors).Replace("\"", "\"\"");
            var code = r.Item?.LibraryCode ?? "";
            var name = r.Item?.Name ?? "";
            sb.AppendLine($"\"{r.RowNumber}\",\"{code}\",\"{name}\",\"{errors}\"");
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        return new FileContentResult(bytes, "text/csv") { FileDownloadName = "import_errors.csv" };
    }

    private static string GetPreviewCacheKey(string previewId) =>
        $"ImportPreview:{previewId}";
}
