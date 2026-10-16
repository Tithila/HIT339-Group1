namespace LibraryManagementSystem.Models;

public class KioskFinePaymentViewModel
{
    public int LoanId { get; set; }

    public string BorrowerName { get; set; } = string.Empty;

    public string ItemName { get; set; } = string.Empty;

    public string LibraryCode { get; set; } = string.Empty;

    public decimal FineAmount { get; set; }

    // These two values are used by the Square Web Payments SDK
    // in the browser.
    public string SquareApplicationId { get; set; } = string.Empty;

    public string SquareLocationId { get; set; } = string.Empty;

    public bool PaymentCompleted { get; set; }

    public string? PaymentId { get; set; }

    public string? ErrorMessage { get; set; }
}