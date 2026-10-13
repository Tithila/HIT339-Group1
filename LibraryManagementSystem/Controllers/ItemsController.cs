
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Data;
using Microsoft.AspNetCore.Authorization;

[Authorize(Roles = "Admin")]
public class ItemsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ItemsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: ITEMS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Items.ToListAsync());
    }

    // GET: ITEMS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var item = await _context.Items
            .FirstOrDefaultAsync(m => m.Id == id);
        if (item == null)
        {
            return NotFound();
        }

        return View(item);
    }

    // GET: ITEMS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: ITEMS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Name,Description,LibraryCode,Status")] Item item)
    {
        if (ModelState.IsValid)
        {
            _context.Add(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(item);
    }

    // GET: ITEMS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var item = await _context.Items.FindAsync(id);
        if (item == null)
        {
            return NotFound();
        }
        return View(item);
    }

    // POST: ITEMS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("Id,Name,Description,LibraryCode,Status")] Item item)
    {
        if (id != item.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(item);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ItemExists(item.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(item);
    }

    // GET: ITEMS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var item = await _context.Items
            .FirstOrDefaultAsync(m => m.Id == id);
        if (item == null)
        {
            return NotFound();
        }

        return View(item);
    }

    // POST: ITEMS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var item = await _context.Items.FindAsync(id);
        if (item != null)
        {
            _context.Items.Remove(item);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool ItemExists(int? id)
    {
        return _context.Items.Any(e => e.Id == id);
    }
}
