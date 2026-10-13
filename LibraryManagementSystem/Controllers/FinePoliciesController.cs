
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Data;
using Microsoft.AspNetCore.Authorization;

[Authorize(Roles = "Admin")]
public class FinePoliciesController : Controller
{
    private readonly ApplicationDbContext _context;

    public FinePoliciesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: FINEPOLICYS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.FinePolicies.ToListAsync());
    }

    // GET: FINEPOLICYS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var finepolicy = await _context.FinePolicies
            .FirstOrDefaultAsync(m => m.Id == id);
        if (finepolicy == null)
        {
            return NotFound();
        }

        return View(finepolicy);
    }

    // GET: FINEPOLICYS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: FINEPOLICYS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,ItemType,DailyRate,GracePeriodDays,MaxFine")] FinePolicy finepolicy)
    {
        if (ModelState.IsValid)
        {
            _context.Add(finepolicy);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(finepolicy);
    }

    // GET: FINEPOLICYS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var finepolicy = await _context.FinePolicies.FindAsync(id);
        if (finepolicy == null)
        {
            return NotFound();
        }
        return View(finepolicy);
    }

    // POST: FINEPOLICYS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("Id,ItemType,DailyRate,GracePeriodDays,MaxFine")] FinePolicy finepolicy)
    {
        if (id != finepolicy.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(finepolicy);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FinePolicyExists(finepolicy.Id))
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
        return View(finepolicy);
    }

    // GET: FINEPOLICYS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var finepolicy = await _context.FinePolicies
            .FirstOrDefaultAsync(m => m.Id == id);
        if (finepolicy == null)
        {
            return NotFound();
        }

        return View(finepolicy);
    }

    // POST: FINEPOLICYS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var finepolicy = await _context.FinePolicies.FindAsync(id);
        if (finepolicy != null)
        {
            _context.FinePolicies.Remove(finepolicy);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool FinePolicyExists(int? id)
    {
        return _context.FinePolicies.Any(e => e.Id == id);
    }
}
