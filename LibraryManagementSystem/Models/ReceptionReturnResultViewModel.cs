namespace LibraryManagementSystem.Models;

public class ReceptionReturnResultViewModel
{
    public int LoanId { get; set; }

    public string BorrowerName { get; set; } = string.Empty;

    public string ItemName { get; set; } = string.Empty;

    public string LibraryCode { get; set; } = string.Empty;

    public ItemConditionResult Condition { get; set; }

    public DateTime ReturnDate { get; set; }

    public decimal FineAmount { get; set; }

    public decimal RepairCost { get; set; }

    public decimal ReplacementCost { get; set; }

    public bool HasOutstandingFine =>
        FineAmount > 0;
}