using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Data;
using Microsoft.AspNetCore.Authorization;

[Authorize(Roles = "Admin")]
public class ToysController : Controller
{
    private readonly ApplicationDbContext _context;

    public ToysController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: TOYS
    public async Task<IActionResult> Index()
    {
        return View(await _context.Toys.ToListAsync());
    }

    // GET: TOYS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var toy = await _context.Toys
            .FirstOrDefaultAsync(m => m.Id == id);

        if (toy == null)
        {
            return NotFound();
        }

        return View(toy);
    }

    // GET: TOYS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: TOYS/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Type,RecommendedAge,Id,Name,Description,LibraryCode,Status")] Toy toy)
    {
        if (await _context.Items.AnyAsync(i =>
            i.LibraryCode == toy.LibraryCode))
        {
            ModelState.AddModelError(
                "LibraryCode",
                "This library code already exists. Please use a unique code.");
        }

        if (ModelState.IsValid)
        {
            _context.Add(toy);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        return View(toy);
    }

    // GET: TOYS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var toy = await _context.Toys.FindAsync(id);

        if (toy == null)
        {
            return NotFound();
        }

        return View(toy);
    }

    // POST: TOYS/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int? id,
        [Bind("Type,RecommendedAge,Id,Name,Description,LibraryCode,Status")] Toy toy)
    {
        if (id != toy.Id)
        {
            return NotFound();
        }

        if (await _context.Items.AnyAsync(i =>
            i.LibraryCode == toy.LibraryCode &&
            i.Id != toy.Id))
        {
            ModelState.AddModelError(
                "LibraryCode",
                "This library code already exists. Please use a unique code.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(toy);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ToyExists(toy.Id))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        return View(toy);
    }

    // GET: TOYS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var toy = await _context.Toys
            .FirstOrDefaultAsync(m => m.Id == id);

        if (toy == null)
        {
            return NotFound();
        }

        return View(toy);
    }

    // POST: TOYS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var toy = await _context.Toys.FindAsync(id);

        if (toy != null)
        {
            _context.Toys.Remove(toy);
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private bool ToyExists(int? id)
    {
        return _context.Toys.Any(e => e.Id == id);
    }
}