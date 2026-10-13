using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services;

public class FineCalculator : IFineCalculator
{
    private readonly ApplicationDbContext _context;

    public FineCalculator(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> CalculateFineAsync(Loan loan)
    {
        if (loan.ReturnDate == null)
        {
            return 0;
        }

        if (loan.Item == null)
        {
            return 0;
        }

        string itemType = loan.Item switch
        {
            Book => "Book",
            Music => "Music",
            Toy => "Toy",
            _ => "Item"
        };

        var policy = await _context.FinePolicies
            .FirstOrDefaultAsync(p => p.ItemType == itemType);

        if (policy == null)
        {
            return 0;
        }

        var overdueDays =
            (loan.ReturnDate.Value.Date - loan.DueDate.Date).Days;

        overdueDays -= policy.GracePeriodDays;

        if (overdueDays <= 0)
        {
            return 0;
        }

        var fine = overdueDays * policy.DailyRate;

        if (fine > policy.MaxFine)
        {
            fine = policy.MaxFine;
        }

        return fine;
    }
}