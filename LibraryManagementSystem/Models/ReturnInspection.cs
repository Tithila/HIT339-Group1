using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

public class ReturnInspection
{
    public int Id { get; set; }

    [Required]
    public int LoanId { get; set; }

    public Loan? Loan { get; set; }

    [Required]
    [Display(Name = "Condition Result")]
    public ItemConditionResult Result { get; set; }

    [StringLength(500)]
    [Display(Name = "Inspection Notes")]
    public string? InspectorNotes { get; set; }

    [Range(0, 10000)]
    [Display(Name = "Estimated Repair Cost")]
    public decimal RepairCost { get; set; }

    [Range(0, 10000)]
    [Display(Name = "Estimated Replacement Cost")]
    public decimal ReplacementCost { get; set; }

    public DateTime InspectionDate { get; set; } = DateTime.Now;
}

public enum ItemConditionResult
{
    Good,
    Damaged,
    Destroyed
}