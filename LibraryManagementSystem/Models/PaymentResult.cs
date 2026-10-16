namespace LibraryManagementSystem.Models;

public class PaymentResult
{
    public bool Success { get; set; }

    public string? PaymentId { get; set; }

    public string? ErrorMessage { get; set; }
}