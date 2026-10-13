using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Models;

[Index(nameof(LibraryCode), IsUnique = true)]

public class Item
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string LibraryCode { get; set; } = string.Empty;

    [Required]
    public ItemStatus Status { get; set; } = ItemStatus.Available;
}

public enum ItemStatus
{
    Available,
    Borrowed,
    Damaged,
    Destroy
}