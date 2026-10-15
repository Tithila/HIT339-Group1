using LibraryManagementSystem.Data;
using LibraryManagementSystem.DTOs;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers.Api;

[ApiController]
[Route("api/items")]
public class ItemsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ItemsApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /api/items
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ItemSearchResultDto>>> GetItems()
    {
        var items = await _context.Items
            .AsNoTracking()
            .OrderBy(i => i.Name)
            .Select(i => new ItemSearchResultDto
            {
                Id = i.Id,
                Name = i.Name,
                Description = i.Description,
                LibraryCode = i.LibraryCode,
                ItemType = EF.Property<string>(i, "Discriminator"),
                Status = i.Status.ToString()
            })
            .ToListAsync();

        return Ok(items);
    }

    // GET: /api/items/available
    [HttpGet("available")]
    public async Task<ActionResult<IEnumerable<ItemSearchResultDto>>> GetAvailableItems()
    {
        var items = await _context.Items
            .AsNoTracking()
            .Where(i => i.Status == ItemStatus.Available)
            .OrderBy(i => i.Name)
            .Select(i => new ItemSearchResultDto
            {
                Id = i.Id,
                Name = i.Name,
                Description = i.Description,
                LibraryCode = i.LibraryCode,
                ItemType = EF.Property<string>(i, "Discriminator"),
                Status = i.Status.ToString()
            })
            .ToListAsync();

        return Ok(items);
    }

    // GET: /api/items/search?query=hobbit
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<ItemSearchResultDto>>> Search(
        [FromQuery] string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest("A search query is required.");
        }

        query = query.Trim();

        var items = await _context.Items
            .AsNoTracking()
            .Where(i =>
                i.Name.Contains(query) ||
                i.LibraryCode.Contains(query) ||
                i.Description.Contains(query))
            .OrderBy(i => i.Name)
            .Select(i => new ItemSearchResultDto
            {
                Id = i.Id,
                Name = i.Name,
                Description = i.Description,
                LibraryCode = i.LibraryCode,
                ItemType = EF.Property<string>(i, "Discriminator"),
                Status = i.Status.ToString()
            })
            .ToListAsync();

        return Ok(items);
    }

    // GET: /api/items/BK001
    [HttpGet("{libraryCode}")]
    public async Task<ActionResult<ItemSearchResultDto>> GetByLibraryCode(
        string libraryCode)
    {
        var item = await _context.Items
            .AsNoTracking()
            .Where(i => i.LibraryCode == libraryCode)
            .Select(i => new ItemSearchResultDto
            {
                Id = i.Id,
                Name = i.Name,
                Description = i.Description,
                LibraryCode = i.LibraryCode,
                ItemType = EF.Property<string>(i, "Discriminator"),
                Status = i.Status.ToString()
            })
            .FirstOrDefaultAsync();

        if (item == null)
        {
            return NotFound();
        }

        return Ok(item);
    }
}