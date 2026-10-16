using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

public class KioskSearchViewModel
{
    [Required]
    [Display(Name = "Library Code")]
    public string LibraryCode { get; set; } = string.Empty;

    [Display(Name = "Library Card Number")]
    public string? LibraryCardNumber { get; set; }

    public Item? Item { get; set; }

    public bool SearchPerformed { get; set; }

    public string? ActionMessage { get; set; }

    public string? ActionError { get; set; }
}