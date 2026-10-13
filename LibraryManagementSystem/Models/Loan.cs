using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

public class Loan
{
    public int Id { get; set; }

    [Required]
    public int BorrowerId { get; set; }

    public Borrower? Borrower { get; set; }

    [Required]
    public int ItemId { get; set; }

    public Item? Item { get; set; }

    public DateTime BorrowDate { get; set; } = DateTime.Now;

    public DateTime DueDate { get; set; }

    public DateTime? ReturnDate { get; set; }

    public bool IsOverdue { get; set; } = false;

    public decimal FineAmount { get; set; } = 0;

    public bool FinePaid { get; set; } = false;
}