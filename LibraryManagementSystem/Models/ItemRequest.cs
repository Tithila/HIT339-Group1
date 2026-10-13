using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

public class ItemRequest
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Item Type")]
    public string Type { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Requester Email")]
    public string RequesterEmail { get; set; } = string.Empty;

    [Required]
    public string Status { get; set; } = "Pending";

    [Display(Name = "Manager Notes")]
    [StringLength(500)]
    public string? ManagerNotes { get; set; }

    public DateTime RequestedDate { get; set; } = DateTime.Now;
}