namespace LibraryManagementSystem.Models;

public class ManagerDashboardViewModel
{
    public int TotalItems { get; set; }
    public int AvailableItems { get; set; }
    public int BorrowedItems { get; set; }
    public int DamagedItems { get; set; }
    public int DestroyedItems { get; set; }

    public int ActiveLoans { get; set; }
    public int OverdueLoans { get; set; }
    public int ReturnedLoans { get; set; }

    public decimal TotalFinesGenerated { get; set; }
    public decimal TotalFinesPaid { get; set; }
    public decimal OutstandingFines { get; set; }

    public int SuspendedBorrowers { get; set; }

    public decimal TotalRepairCost { get; set; }
    public decimal TotalReplacementCost { get; set; }

}