
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Admin")]
public class MusicController : Controller
{
    private readonly ApplicationDbContext _context;

    public MusicController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Load branches for the Create form
    private async Task LoadBranchesAsync()
    {
        var branches = await _context.Branches
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .ToListAsync();

        ViewBag.Branches = new SelectList(
            branches, "Id", "Name");
    }

    // Validate the selected branch
    private async Task ValidateBranchAsync(int? branchId)
    {
        if (!branchId.HasValue ||
            !await _context.Branches.AnyAsync(
                b => b.Id == branchId.Value))
        {
            ModelState.AddModelError(
                "BranchId",
                "Please select a valid library branch.");
        }
    }

    // GET: Music
    public async Task<IActionResult> Index()
    {
        var musicItems = await _context.MusicItems
            .Include(m => m.Branch)
            .AsNoTracking()
            .ToListAsync();

        return View(musicItems);
    }

    // GET: Music/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var music = await _context.MusicItems
            .Include(m => m.Branch)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (music == null)
        {
            return NotFound();
        }

        return View(music);
    }

    // GET: Music/Create
    public async Task<IActionResult> Create()
    {
        await LoadBranchesAsync();

        return View();
    }

    // POST: Music/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Artist,Year,Name,Description,LibraryCode,Status,BranchId")]
        Music music)
    {
        if (await _context.Items.AnyAsync(i =>
            i.LibraryCode == music.LibraryCode))
        {
            ModelState.AddModelError(
                "LibraryCode",
                "This library code already exists. Please use a unique code.");
        }

        await ValidateBranchAsync(music.BranchId);

        if (ModelState.IsValid)
        {
            _context.MusicItems.Add(music);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        await LoadBranchesAsync();

        return View(music);
    }

    // GET: Music/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var music = await _context.MusicItems
            .Include(m => m.Branch)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (music == null)
        {
            return NotFound();
        }

        return View(music);
    }

    // POST: Music/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int? id,
        [Bind("Id,Artist,Year,Name,Description,LibraryCode,Status")]
        Music input)
    {
        if (id == null || id != input.Id)
        {
            return NotFound();
        }

        var music = await _context.MusicItems
            .FindAsync(id);

        if (music == null)
        {
            return NotFound();
        }

        if (await _context.Items.AnyAsync(i =>
            i.LibraryCode == input.LibraryCode &&
            i.Id != input.Id))
        {
            ModelState.AddModelError(
                "LibraryCode",
                "This library code already exists. Please use a unique code.");
        }

        // BranchId is intentionally not editable here.
        // Existing items must move through the transfer workflow.

        if (ModelState.IsValid)
        {
            music.Artist = input.Artist;
            music.Year = input.Year;
            music.Name = input.Name;
            music.Description = input.Description;
            music.LibraryCode = input.LibraryCode;
            music.Status = input.Status;

            try
            {
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.MusicItems
                    .AnyAsync(m => m.Id == input.Id))
                {
                    return NotFound();
                }

                throw;
            }
        }

        // Preserve the current branch for the Edit view.
        input.BranchId = music.BranchId;
        input.Branch = await _context.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == music.BranchId);

        return View(input);
    }

    // GET: Music/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var music = await _context.MusicItems
            .Include(m => m.Branch)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (music == null)
        {
            return NotFound();
        }

        return View(music);
    }

    // POST: Music/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var music = await _context.MusicItems
            .FindAsync(id);

        if (music != null)
        {
            _context.MusicItems.Remove(music);

            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}
