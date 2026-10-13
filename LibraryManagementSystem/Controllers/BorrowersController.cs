
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Data;
using Microsoft.AspNetCore.Authorization;

[Authorize(Roles = "Reception")]
public class BorrowersController : Controller
{
    private readonly ApplicationDbContext _context;

    public BorrowersController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: BORROWERS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Borrowers.ToListAsync());
    }

    // GET: BORROWERS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var borrower = await _context.Borrowers
            .FirstOrDefaultAsync(m => m.Id == id);
        if (borrower == null)
        {
            return NotFound();
        }

        return View(borrower);
    }

    // GET: BORROWERS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: BORROWERS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,FullName,Email,Phone,IsSuspended,SuspensionReason,Loans")] Borrower borrower)
    {
        if (ModelState.IsValid)
        {
            _context.Add(borrower);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(borrower);
    }

    // GET: BORROWERS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var borrower = await _context.Borrowers.FindAsync(id);
        if (borrower == null)
        {
            return NotFound();
        }
        return View(borrower);
    }

    // POST: BORROWERS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("Id,FullName,Email,Phone,IsSuspended,SuspensionReason,Loans")] Borrower borrower)
    {
        if (id != borrower.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(borrower);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BorrowerExists(borrower.Id))
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
        return View(borrower);
    }

    // GET: BORROWERS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var borrower = await _context.Borrowers
            .FirstOrDefaultAsync(m => m.Id == id);
        if (borrower == null)
        {
            return NotFound();
        }

        return View(borrower);
    }

    // POST: BORROWERS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var borrower = await _context.Borrowers.FindAsync(id);
        if (borrower != null)
        {
            _context.Borrowers.Remove(borrower);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool BorrowerExists(int? id)
    {
        return _context.Borrowers.Any(e => e.Id == id);
    }
}
