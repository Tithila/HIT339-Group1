using LibraryManagementSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services;

public class BorrowerEligibilityService : IBorrowerEligibilityService
{
    private readonly ApplicationDbContext _context;

    public BorrowerEligibilityService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task CheckAndUpdateSuspensionAsync(int borrowerId)
    {
        var borrower = await _context.Borrowers
            .FirstOrDefaultAsync(b => b.Id == borrowerId);

        if (borrower == null)
        {
            return;
        }

        var unpaidFines = await _context.Loans
            .Where(l =>
                l.BorrowerId == borrowerId &&
                !l.FinePaid)
            .SumAsync(l => l.FineAmount);

        var overdueCount = await _context.Loans
            .CountAsync(l =>
                l.BorrowerId == borrowerId &&
                l.IsOverdue);

        if (unpaidFines >= 50 || overdueCount >= 3)
        {
            borrower.IsSuspended = true;

            borrower.SuspensionReason =
                $"Outstanding fines: ${unpaidFines:F2}, overdue loans: {overdueCount}";
        }
        else
        {
            borrower.IsSuspended = false;
            borrower.SuspensionReason = null;
        }

        await _context.SaveChangesAsync();
    }
}