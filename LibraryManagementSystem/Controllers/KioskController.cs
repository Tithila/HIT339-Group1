using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers;

public class KioskController : Controller
{
    private readonly ApplicationDbContext _context;

    public KioskController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Kiosk
    [HttpGet]
    public IActionResult Index()
    {
        return View(new KioskSearchViewModel());
    }

    // POST: /Kiosk
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(KioskSearchViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var code = model.LibraryCode.Trim();

        var item = await _context.Items
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.LibraryCode == code);

        model.SearchPerformed = true;
        model.Item = item;

        return View(model);
    }
}