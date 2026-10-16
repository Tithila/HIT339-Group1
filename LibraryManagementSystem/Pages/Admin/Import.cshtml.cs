using System.Text;
using System.Text.Json;
using LibraryManagementSystem.Services;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibraryManagementSystem.Pages.Admin;

[Authorize(Roles = "Admin,Manager")]
public class ImportModel : PageModel
{
    private readonly IImportService _importService;

    public ImportModel(IImportService importService)
    {
        _importService = importService;
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
            TempData["ImportPreview"] = JsonSerializer.Serialize(Preview);
            return RedirectToPage("/Admin/ImportPreview");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostConfirmAsync()
    {
        if (!TempData.TryGetValue("ImportPreview", out var obj) || obj == null)
        {
            ModelState.AddModelError(string.Empty, "No preview available to confirm. Please upload and preview again.");
            return Page();
        }

        var json = obj.ToString();
        var preview = JsonSerializer.Deserialize<ImportPreviewViewModel>(json ?? string.Empty);
        if (preview == null)
        {
            ModelState.AddModelError(string.Empty, "Preview deserialization failed.");
            return Page();
        }

        var result = await _importService.ConfirmImportAsync(preview);
        TempData["ImportResult"] = $"Imported: {result.ImportedCount}, Failed: {result.FailedCount}";

        return RedirectToPage("/Admin/Import");
    }

    public IActionResult OnPostDownloadErrors()
    {
        var json = TempData.Peek("ImportPreview") as string;
        if (string.IsNullOrEmpty(json))
        {
            return RedirectToPage();
        }

        var preview = JsonSerializer.Deserialize<ImportPreviewViewModel>(json ?? string.Empty);
        if (preview == null)
        {
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
}
