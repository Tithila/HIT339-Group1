
using System;
using System.Linq;
using System.Threading.Tasks;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BooksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Books
        public async Task<IActionResult> Index()
        {
            var books = await _context.Books
                .Include(b => b.Branch)
                .AsNoTracking()
                .OrderBy(b => b.Name)
                .ToListAsync();

            return View(books);
        }

        // GET: Books/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Branch)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id.Value);

            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // GET: Books/Create
        public async Task<IActionResult> Create()
        {
            await LoadBranchesAsync();

            return View();
        }

        // POST: Books/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,Description,LibraryCode,Status,Author,Genre,BranchId")]
            Book book)
        {
            if (book.BranchId == null)
            {
                ModelState.AddModelError(
                    nameof(Book.BranchId),
                    "Please select a library branch.");
            }
            else
            {
                bool branchExists = await _context.Branches
                    .AnyAsync(b => b.Id == book.BranchId.Value);

                if (!branchExists)
                {
                    ModelState.AddModelError(
                        nameof(Book.BranchId),
                        "The selected library branch is invalid.");
                }
            }

            bool codeExists = await _context.Items
                .AnyAsync(i => i.LibraryCode == book.LibraryCode);

            if (codeExists)
            {
                ModelState.AddModelError(
                    nameof(Book.LibraryCode),
                    "This library code is already in use.");
            }

            // Navigation properties are loaded by EF,
            // not entered directly through the form.
            ModelState.Remove(nameof(Book.Branch));

            if (!ModelState.IsValid)
            {
                await LoadBranchesAsync(book.BranchId);

                return View(book);
            }

            _context.Books.Add(book);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    nameof(Book.LibraryCode),
                    "Unable to save the book. Check that its library code is unique.");

                await LoadBranchesAsync(book.BranchId);

                return View(book);
            }

            TempData["SuccessMessage"] =
                "Book created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Books/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Branch)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id.Value);

            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // POST: Books/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Name,Description,LibraryCode,Status,Author,Genre")]
            Book input)
        {
            if (id != input.Id)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Branch)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null)
            {
                return NotFound();
            }

            bool codeExists = await _context.Items
                .AnyAsync(i =>
                    i.LibraryCode == input.LibraryCode &&
                    i.Id != id);

            if (codeExists)
            {
                ModelState.AddModelError(
                    nameof(Book.LibraryCode),
                    "This library code is already in use.");
            }

            // Branch cannot be changed from the Edit form.
            // Branch movement must use the transfer workflow.
            ModelState.Remove(nameof(Book.Branch));
            ModelState.Remove(nameof(Book.BranchId));

            if (!ModelState.IsValid)
            {
                input.BranchId = book.BranchId;
                input.Branch = book.Branch;

                return View(input);
            }

            book.Name = input.Name;
            book.Description = input.Description;
            book.LibraryCode = input.LibraryCode;
            book.Status = input.Status;
            book.Author = input.Author;
            book.Genre = input.Genre;

            // Do not update book.BranchId.

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await BookExistsAsync(id))
                {
                    return NotFound();
                }

                throw;
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    nameof(Book.LibraryCode),
                    "Unable to save changes. Check that the library code is unique.");

                input.BranchId = book.BranchId;
                input.Branch = book.Branch;

                return View(input);
            }

            TempData["SuccessMessage"] =
                "Book updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Books/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Branch)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id.Value);

            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // POST: Books/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Books
                .FindAsync(id);

            if (book == null)
            {
                return RedirectToAction(nameof(Index));
            }

            _context.Books.Remove(book);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] =
                    "This book cannot be deleted because it may have related records.";

                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] =
                "Book deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadBranchesAsync(int? selectedBranchId = null)
        {
            var branches = await _context.Branches
                .AsNoTracking()
                .OrderBy(b => b.Name)
                .ToListAsync();

            ViewBag.Branches = new SelectList(
                branches,
                "Id",
                "Name",
                selectedBranchId);
        }

        private async Task<bool> BookExistsAsync(int id)
        {
            return await _context.Books
                .AnyAsync(b => b.Id == id);
        }
    }
}
