using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

public class Book : Item
{
    [Required]
    [StringLength(100)]
    public string Author { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Genre { get; set; } = string.Empty;
}