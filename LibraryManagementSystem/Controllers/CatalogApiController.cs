using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/catalog")]
public class CatalogApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CatalogApiController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("items")]
    public async Task<IActionResult> GetItems(
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] bool availableOnly = false)
    {
        var query = _db.Items
            .AsNoTracking()
            .Include(item => item.Branch)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(item =>
                item.Name.Contains(search) ||
                item.LibraryCode.Contains(search));
        }

        if (availableOnly)
        {
            query = query.Where(item => item.Status == ItemStatus.Available);
        }

        var items = await query
            .OrderBy(item => item.Name)
            .ToListAsync();

        var result = items
            .Where(item => string.IsNullOrWhiteSpace(category) ||
                GetCategory(item) == category)
            .Select(item => new
            {
                item.Id,
                item.LibraryCode,
                item.Name,
                item.Description,
                Category = GetCategory(item),
                Status = item.Status.ToString(),
                Branch = item.Branch == null
                    ? null
                    : new
                    {
                        item.Branch.Code,
                        item.Branch.Name
                    }
            });

        return Ok(result);
    }

    [HttpGet("items/{id:int}")]
    public async Task<IActionResult> GetItem(int id)
    {
        var item = await _db.Items
            .AsNoTracking()
            .Include(item => item.Branch)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (item == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            item.Id,
            item.LibraryCode,
            item.Name,
            item.Description,
            Category = GetCategory(item),
            Status = item.Status.ToString(),
            Branch = item.Branch == null
                ? null
                : new
                {
                    item.Branch.Code,
                    item.Branch.Name
                }
        });
    }

    [HttpGet("categories")]
    public IActionResult GetCategories()
    {
        return Ok(new[] { "Book", "Music", "Toy" });
    }

    [HttpGet("branches")]
    public async Task<IActionResult> GetBranches()
    {
        var branches = await _db.Branches
            .AsNoTracking()
            .OrderBy(branch => branch.Name)
            .Select(branch => new
            {
                branch.Id,
                branch.Code,
                branch.Name,
                branch.Address,
                branch.OpeningHours
            })
            .ToListAsync();

        return Ok(branches);
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var status = await _db.Branches
            .AsNoTracking()
            .Select(branch => new
            {
                branch.Code,
                branch.Name,
                ItemCount = branch.Items.Count,
                AvailableItems = branch.Items.Count(
                    item => item.Status == ItemStatus.Available)
            })
            .ToListAsync();

        return Ok(status);
    }

    private static string GetCategory(Item item)
    {
        return item switch
        {
            Book => "Book",
            Music => "Music",
            Toy => "Toy",
            _ => "Item"
        };
    }
}
