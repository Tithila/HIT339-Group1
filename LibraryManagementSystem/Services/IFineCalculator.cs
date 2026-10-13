using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Services;

public interface IFineCalculator
{
    Task<decimal> CalculateFineAsync(Loan loan);
}