namespace LibraryManagementSystem.Models;

public class FinePaymentViewModel
{
    public int LoanId { get; set; }

    public string BorrowerName { get; set; } = string.Empty;

    public string ItemName { get; set; } = string.Empty;

    public string LibraryCode { get; set; } = string.Empty;

    public decimal FineAmount { get; set; }

    public DateTime? ReturnDate { get; set; }
}