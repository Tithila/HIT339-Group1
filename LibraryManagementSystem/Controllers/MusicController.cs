using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Data;
using Microsoft.AspNetCore.Authorization;

[Authorize(Roles = "Admin")]
public class MusicController : Controller
{
    private readonly ApplicationDbContext _context;

    public MusicController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: MUSIC
    public async Task<IActionResult> Index()
    {
        return View(await _context.MusicItems.ToListAsync());
    }

    // GET: MUSIC/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var music = await _context.MusicItems
            .FirstOrDefaultAsync(m => m.Id == id);

        if (music == null)
        {
            return NotFound();
        }

        return View(music);
    }

    // GET: MUSIC/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: MUSIC/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Artist,Year,Id,Name,Description,LibraryCode,Status")] Music music)
    {
        if (await _context.Items.AnyAsync(i =>
            i.LibraryCode == music.LibraryCode))
        {
            ModelState.AddModelError(
                "LibraryCode",
                "This library code already exists. Please use a unique code.");
        }

        if (ModelState.IsValid)
        {
            _context.Add(music);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        return View(music);
    }

    // GET: MUSIC/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var music = await _context.MusicItems.FindAsync(id);

        if (music == null)
        {
            return NotFound();
        }

        return View(music);
    }

    // POST: MUSIC/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int? id,
        [Bind("Artist,Year,Id,Name,Description,LibraryCode,Status")] Music music)
    {
        if (id != music.Id)
        {
            return NotFound();
        }

        if (await _context.Items.AnyAsync(i =>
            i.LibraryCode == music.LibraryCode &&
            i.Id != music.Id))
        {
            ModelState.AddModelError(
                "LibraryCode",
                "This library code already exists. Please use a unique code.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(music);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MusicExists(music.Id))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        return View(music);
    }

    // GET: MUSIC/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var music = await _context.MusicItems
            .FirstOrDefaultAsync(m => m.Id == id);

        if (music == null)
        {
            return NotFound();
        }

        return View(music);
    }

    // POST: MUSIC/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var music = await _context.MusicItems.FindAsync(id);

        if (music != null)
        {
            _context.MusicItems.Remove(music);
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private bool MusicExists(int? id)
    {
        return _context.MusicItems.Any(e => e.Id == id);
    }
}