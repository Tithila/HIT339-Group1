using LibraryManagementSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services;

public class BorrowerEligibilityService : IBorrowerEligibilityService
{
    private readonly ApplicationDbContext _context;

    public BorrowerEligibilityService(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task CheckAndUpdateSuspensionAsync(
        int borrowerId)
    {
        var borrower = await _context.Borrowers
            .FirstOrDefaultAsync(
                b => b.Id == borrowerId);

        if (borrower == null)
        {
            return;
        }

        var today = DateTime.Today;

        // Only fines that actually have an amount owing
        // and have not been paid.
        var unpaidFines = await _context.Loans
            .Where(l =>
                l.BorrowerId == borrowerId &&
                l.FineAmount > 0 &&
                !l.FinePaid)
            .SumAsync(l => l.FineAmount);

        // Only count CURRENTLY ACTIVE overdue loans.
        // Returned historical overdue loans must not
        // continue suspending the borrower.
        var activeOverdueCount = await _context.Loans
            .CountAsync(l =>
                l.BorrowerId == borrowerId &&
                l.ReturnDate == null &&
                l.DueDate.Date < today);

        var suspendForFines =
            unpaidFines >= 50;

        var suspendForOverdueLoans =
            activeOverdueCount >= 3;

        borrower.IsSuspended =
            suspendForFines ||
            suspendForOverdueLoans;

        if (borrower.IsSuspended)
        {
            borrower.SuspensionReason =
                $"Outstanding fines: ${unpaidFines:F2}, " +
                $"active overdue loans: {activeOverdueCount}";
        }
        else
        {
            borrower.SuspensionReason = null;
        }

        await _context.SaveChangesAsync();
    }
}