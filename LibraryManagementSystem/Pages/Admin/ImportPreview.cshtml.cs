using System.Text.Json;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibraryManagementSystem.Pages.Admin;

[Authorize(Roles = "Admin,Manager")]
public class ImportPreviewModel : PageModel
{
    public ImportPreviewViewModel? Preview { get; private set; }

    public IActionResult OnGet()
    {
        var json = TempData.Peek("ImportPreview") as string;
        if (string.IsNullOrWhiteSpace(json))
        {
            return RedirectToPage("/Admin/Import");
        }

        Preview = JsonSerializer.Deserialize<ImportPreviewViewModel>(json);
        return Preview == null
            ? RedirectToPage("/Admin/Import")
            : Page();
    }
}