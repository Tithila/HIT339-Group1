using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

public class KioskSearchViewModel
{
    [Required]
    [Display(Name = "Library Code")]
    public string LibraryCode { get; set; } = string.Empty;

    public Item? Item { get; set; }

    public bool SearchPerformed { get; set; }
}