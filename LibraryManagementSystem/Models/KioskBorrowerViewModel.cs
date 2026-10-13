using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

public class KioskBorrowerViewModel
{
    [Required]
    [Display(Name = "Library Card Number")]
    public string LibraryCardNumber { get; set; } = string.Empty;

    public Borrower? Borrower { get; set; }

    public bool SearchPerformed { get; set; }
}