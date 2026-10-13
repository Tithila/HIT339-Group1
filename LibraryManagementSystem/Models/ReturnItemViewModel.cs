using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

public class ReturnItemViewModel
{
    [Required]
    [Display(Name = "Library Code")]
    public string LibraryCode { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Condition")]
    public ItemConditionResult Condition { get; set; }

    [Display(Name = "Inspection Notes")]
    [StringLength(500)]
    public string? InspectorNotes { get; set; }

    [Range(0, 10000)]
    [Display(Name = "Estimated Repair Cost")]
    public decimal RepairCost { get; set; }

    [Range(0, 10000)]
    [Display(Name = "Estimated Replacement Cost")]
    public decimal ReplacementCost { get; set; }
}