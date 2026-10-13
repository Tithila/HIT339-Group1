using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

public class KioskCheckoutViewModel
{
    [Required]
    [Display(Name = "Library Code")]
    public string LibraryCode { get; set; } = string.Empty;

    public Borrower? Borrower { get; set; }

    public Item? Item { get; set; }

    public bool ItemScanned { get; set; }

    public bool CheckoutCompleted { get; set; }

    public DateTime? DueDate { get; set; }

    public int? LoanId { get; set; }
}