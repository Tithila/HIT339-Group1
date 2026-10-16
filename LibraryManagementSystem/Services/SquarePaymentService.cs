using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Square;
using Square.Payments;

namespace LibraryManagementSystem.Services;

public class SquarePaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly IBorrowerEligibilityService _borrowerEligibilityService;
    private readonly IAuditService _auditService;
    private readonly SquareSettings _settings;
    private readonly SquareClient _squareClient;

    public SquarePaymentService(
        ApplicationDbContext context,
        IBorrowerEligibilityService borrowerEligibilityService,
        IAuditService auditService,
        IOptions<SquareSettings> options)
    {
        _context = context;
        _borrowerEligibilityService = borrowerEligibilityService;
        _auditService = auditService;
        _settings = options.Value;

        _squareClient = new SquareClient(
            _settings.AccessToken,
            new ClientOptions
            {
                BaseUrl = SquareEnvironment.Sandbox
            });
    }

    public async Task<PaymentResult> PayFineAsync(
        int loanId,
        string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            return new PaymentResult
            {
                Success = false,
                ErrorMessage = "A Square payment token is required."
            };
        }

        var loan = await _context.Loans
            .Include(l => l.Borrower)
            .Include(l => l.Item)
            .FirstOrDefaultAsync(l => l.Id == loanId);

        if (loan == null)
        {
            return new PaymentResult
            {
                Success = false,
                ErrorMessage = "The loan could not be found."
            };
        }

        if (loan.FineAmount <= 0)
        {
            return new PaymentResult
            {
                Success = false,
                ErrorMessage = "This loan does not have an outstanding fine."
            };
        }

        if (loan.FinePaid)
        {
            return new PaymentResult
            {
                Success = false,
                ErrorMessage = "This fine has already been paid."
            };
        }

        if (string.IsNullOrWhiteSpace(_settings.AccessToken) ||
            string.IsNullOrWhiteSpace(_settings.LocationId))
        {
            return new PaymentResult
            {
                Success = false,
                ErrorMessage = "Square payment configuration is incomplete."
            };
        }

        // Square expects monetary values in the smallest currency unit.
        // Example: $2.00 AUD = 200 cents.
        long amountInCents =
            decimal.ToInt64(
                decimal.Round(
                    loan.FineAmount * 100m,
                    0,
                    MidpointRounding.AwayFromZero));

        var request = new CreatePaymentRequest
        {
            SourceId = sourceId,

            // Every Square payment request needs a unique idempotency key.
            IdempotencyKey = Guid.NewGuid().ToString(),

            AmountMoney = new Money
            {
                Amount = amountInCents,
                Currency = Currency.Aud
            },

            LocationId = _settings.LocationId,

            Autocomplete = true,

            ReferenceId = $"loan-{loan.Id}",

            Note =
                $"Library fine payment for loan {loan.Id}"
        };

        try
        {
            var response =
                await _squareClient.Payments.CreateAsync(request);

            var payment = response.Payment;

            if (payment == null ||
                payment.Status != "COMPLETED")
            {
                return new PaymentResult
                {
                    Success = false,
                    PaymentId = payment?.Id,
                    ErrorMessage =
                        "Square did not complete the payment."
                };
            }

            // Only mark the fine paid after Square confirms payment.
            loan.FinePaid = true;

            await _context.SaveChangesAsync();

            // Paying the fine may remove a borrower suspension.
            await _borrowerEligibilityService
                .CheckAndUpdateSuspensionAsync(
                    loan.BorrowerId);

            await _auditService.LogAsync(
                "Fine Paid - Square",
                "Loan",
                loan.Id,
                $"Fine of ${loan.FineAmount:F2} paid through Square " +
                $"for {loan.Item?.Name} ({loan.Item?.LibraryCode}). " +
                $"Square Payment ID: {payment.Id}",
                loan.Borrower?.LibraryCardNumber
                    ?? "Self-Service Payment");

            return new PaymentResult
            {
                Success = true,
                PaymentId = payment.Id
            };
        }
        catch (SquareApiException ex)
        {
            return new PaymentResult
            {
                Success = false,
                ErrorMessage =
                    $"Square payment failed: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new PaymentResult
            {
                Success = false,
                ErrorMessage =
                    $"Payment could not be processed: {ex.Message}"
            };
        }
    }
}