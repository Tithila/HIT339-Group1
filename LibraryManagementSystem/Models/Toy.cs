using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

public class Toy : Item
{
    [Required]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty;

    public int RecommendedAge { get; set; }
}