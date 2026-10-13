
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Admin")]
public class ToysController : Controller
{
    private readonly ApplicationDbContext _context;

    public ToysController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Load available branches for the Create form
    private async Task LoadBranchesAsync()
    {
        var branches = await _context.Branches
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .ToListAsync();

        ViewBag.Branches = new SelectList(
            branches, "Id", "Name");
    }

    // Validate the selected library branch
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

    // GET: Toys
    public async Task<IActionResult> Index()
    {
        var toys = await _context.Toys
            .Include(t => t.Branch)
            .AsNoTracking()
            .ToListAsync();

        return View(toys);
    }

    // GET: Toys/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var toy = await _context.Toys
            .Include(t => t.Branch)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (toy == null)
        {
            return NotFound();
        }

        return View(toy);
    }

    // GET: Toys/Create
    public async Task<IActionResult> Create()
    {
        await LoadBranchesAsync();

        return View();
    }

    // POST: Toys/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Type,RecommendedAge,Name,Description,LibraryCode,Status,BranchId")]
        Toy toy)
    {
        if (await _context.Items.AnyAsync(i =>
            i.LibraryCode == toy.LibraryCode))
        {
            ModelState.AddModelError(
                "LibraryCode",
                "This library code already exists. Please use a unique code.");
        }

        await ValidateBranchAsync(toy.BranchId);

        if (ModelState.IsValid)
        {
            _context.Toys.Add(toy);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        await LoadBranchesAsync();

        return View(toy);
    }

    // GET: Toys/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var toy = await _context.Toys
            .Include(t => t.Branch)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (toy == null)
        {
            return NotFound();
        }

        return View(toy);
    }

    // POST: Toys/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int? id,
        [Bind("Id,Type,RecommendedAge,Name,Description,LibraryCode,Status")]
        Toy input)
    {
        if (id == null || id != input.Id)
        {
            return NotFound();
        }

        var toy = await _context.Toys.FindAsync(id);

        if (toy == null)
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

        // BranchId is intentionally excluded from Edit.
        // Existing items must move through the transfer workflow.

        if (ModelState.IsValid)
        {
            toy.Type = input.Type;
            toy.RecommendedAge = input.RecommendedAge;
            toy.Name = input.Name;
            toy.Description = input.Description;
            toy.LibraryCode = input.LibraryCode;
            toy.Status = input.Status;

            try
            {
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Toys
                    .AnyAsync(t => t.Id == input.Id))
                {
                    return NotFound();
                }

                throw;
            }
        }

        // Restore branch information when validation fails.
        input.BranchId = toy.BranchId;

        input.Branch = await _context.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == toy.BranchId);

        return View(input);
    }

    // GET: Toys/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var toy = await _context.Toys
            .Include(t => t.Branch)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (toy == null)
        {
            return NotFound();
        }

        return View(toy);
    }

    // POST: Toys/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var toy = await _context.Toys.FindAsync(id);

        if (toy != null)
        {
            _context.Toys.Remove(toy);

            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}
