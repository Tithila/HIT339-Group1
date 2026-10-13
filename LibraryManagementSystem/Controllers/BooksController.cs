using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Data;
using Microsoft.AspNetCore.Authorization;

[Authorize(Roles = "Admin")]
public class BooksController : Controller
{
    private readonly ApplicationDbContext _context;

    public BooksController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: BOOKS
    public async Task<IActionResult> Index()
    {
        return View(await _context.Books.ToListAsync());
    }

    // GET: BOOKS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var book = await _context.Books
            .FirstOrDefaultAsync(m => m.Id == id);

        if (book == null)
        {
            return NotFound();
        }

        return View(book);
    }

    // GET: BOOKS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: BOOKS/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Author,Genre,Id,Name,Description,LibraryCode,Status")] Book book)
    {
        if (await _context.Items.AnyAsync(i =>
            i.LibraryCode == book.LibraryCode))
        {
            ModelState.AddModelError(
                "LibraryCode",
                "This library code already exists. Please use a unique code.");
        }

        if (ModelState.IsValid)
        {
            _context.Add(book);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        return View(book);
    }

    // GET: BOOKS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var book = await _context.Books.FindAsync(id);

        if (book == null)
        {
            return NotFound();
        }

        return View(book);
    }

    // POST: BOOKS/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int? id,
        [Bind("Author,Genre,Id,Name,Description,LibraryCode,Status")] Book book)
    {
        if (id != book.Id)
        {
            return NotFound();
        }

        if (await _context.Items.AnyAsync(i =>
            i.LibraryCode == book.LibraryCode &&
            i.Id != book.Id))
        {
            ModelState.AddModelError(
                "LibraryCode",
                "This library code already exists. Please use a unique code.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(book);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BookExists(book.Id))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        return View(book);
    }

    // GET: BOOKS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var book = await _context.Books
            .FirstOrDefaultAsync(m => m.Id == id);

        if (book == null)
        {
            return NotFound();
        }

        return View(book);
    }

    // POST: BOOKS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var book = await _context.Books.FindAsync(id);

        if (book != null)
        {
            _context.Books.Remove(book);
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private bool BookExists(int? id)
    {
        return _context.Books.Any(e => e.Id == id);
    }
}