using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

namespace LibraryManagementSystem.Pages.Admin;

[Authorize(Roles = "Admin,Manager")]
public class ImportPreviewModel : PageModel
{
    private readonly IMemoryCache _cache;

    public ImportPreviewModel(IMemoryCache cache)
    {
        _cache = cache;
    }

    public ImportPreviewViewModel? Preview { get; private set; }

    public IActionResult OnGet()
    {
        var previewId = TempData.Peek("ImportPreviewId") as string;
        if (string.IsNullOrWhiteSpace(previewId) ||
            !_cache.TryGetValue($"ImportPreview:{previewId}", out ImportPreviewViewModel? preview) ||
            preview == null)
        {
            TempData["ImportError"] = "The preview has expired. Please upload and preview the CSV again.";
            return RedirectToPage("/Admin/Import");
        }

        Preview = preview;
        return Page();
    }
}