
using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

public class Branch
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Address { get; set; } = string.Empty;

    [StringLength(100)]
    public string? OpeningHours { get; set; }

    public ICollection<Item> Items { get; set; } = new List<Item>();
}
