using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

public class Borrower
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string Phone { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Library Card Number")]
    public string? LibraryCardNumber { get; set; }

    public bool IsSuspended { get; set; } = false;

    public string? SuspensionReason { get; set; }

    public List<Loan> Loans { get; set; } = new();
}