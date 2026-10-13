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

    // -------------------------
    // Item lookup
    // GET: /Kiosk
    // -------------------------

    [HttpGet]
    public IActionResult Index()
    {
        return View(new KioskSearchViewModel());
    }

    // -------------------------
    // Item lookup
    // POST: /Kiosk
    // -------------------------

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

    // -------------------------
    // Borrower card lookup
    // GET: /Kiosk/Borrower
    // -------------------------

    [HttpGet]
    public IActionResult Borrower()
    {
        return View(new KioskBorrowerViewModel());
    }

    // -------------------------
    // Borrower card lookup
    // POST: /Kiosk/Borrower
    // -------------------------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Borrower(KioskBorrowerViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var cardNumber = model.LibraryCardNumber.Trim();

        var borrower = await _context.Borrowers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                b => b.LibraryCardNumber == cardNumber);

        model.SearchPerformed = true;
        model.Borrower = borrower;

        return View(model);
    }
}