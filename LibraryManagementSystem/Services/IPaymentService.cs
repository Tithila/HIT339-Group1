using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Services;

public interface IPaymentService
{
    Task<PaymentResult> PayFineAsync(
        int loanId,
        string sourceId);
}