using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers;

public class KioskController : Controller
{
    private readonly ApplicationDbContext _context;

    private const string KioskBorrowerSessionKey = "KioskBorrowerId";

    public KioskController(ApplicationDbContext context)
    {
        _context = context;
    }

    // -------------------------
    // Item lookup
    // GET: /Kiosk
    // -------------------------

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var borrowerId =
            HttpContext.Session.GetInt32(KioskBorrowerSessionKey);

        if (borrowerId == null)
        {
            return RedirectToAction(nameof(Borrower));
        }

        var borrower = await _context.Borrowers
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == borrowerId.Value);

        if (borrower == null || borrower.IsSuspended)
        {
            HttpContext.Session.Remove(KioskBorrowerSessionKey);

            return RedirectToAction(nameof(Borrower));
        }

        ViewBag.KioskBorrower = borrower;

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
        var borrowerId =
            HttpContext.Session.GetInt32(KioskBorrowerSessionKey);

        if (borrowerId == null)
        {
            return RedirectToAction(nameof(Borrower));
        }

        var borrower = await _context.Borrowers
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == borrowerId.Value);

        if (borrower == null || borrower.IsSuspended)
        {
            HttpContext.Session.Remove(KioskBorrowerSessionKey);

            return RedirectToAction(nameof(Borrower));
        }

        ViewBag.KioskBorrower = borrower;

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
    public async Task<IActionResult> Borrower(
        KioskBorrowerViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var cardNumber =
            model.LibraryCardNumber.Trim();

        var borrower = await _context.Borrowers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                b => b.LibraryCardNumber == cardNumber);

        model.SearchPerformed = true;
        model.Borrower = borrower;

        if (borrower != null && !borrower.IsSuspended)
        {
            HttpContext.Session.SetInt32(
                KioskBorrowerSessionKey,
                borrower.Id);
        }
        else
        {
            HttpContext.Session.Remove(
                KioskBorrowerSessionKey);
        }

        return View(model);
    }

    // -------------------------
    // End kiosk session
    // GET: /Kiosk/Cancel
    // -------------------------

    [HttpGet]
    public IActionResult Cancel()
    {
        HttpContext.Session.Remove(
            KioskBorrowerSessionKey);

        return RedirectToAction(nameof(Borrower));
    }
}