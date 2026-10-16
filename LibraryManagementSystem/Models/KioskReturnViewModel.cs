using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

public class KioskReturnViewModel
{
    [Required]
    [Display(Name = "Library Code")]
    public string LibraryCode { get; set; } = string.Empty;

    public Item? Item { get; set; }

    public Loan? Loan { get; set; }

    public bool ItemScanned { get; set; }

    public bool ReturnCompleted { get; set; }

    public DateTime? ReturnDate { get; set; }

    public decimal FineAmount { get; set; }

    public bool FineRequired => FineAmount > 0;
}