using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

public class FinePolicy
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Item Type")]
    public string ItemType { get; set; } = string.Empty;

    [Range(0, 100)]
    [Display(Name = "Daily Fine Rate")]
    public decimal DailyRate { get; set; }

    [Range(0, 30)]
    [Display(Name = "Grace Period Days")]
    public int GracePeriodDays { get; set; }

    [Range(0, 1000)]
    [Display(Name = "Maximum Fine")]
    public decimal MaxFine { get; set; }
}