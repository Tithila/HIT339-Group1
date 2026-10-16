namespace LibraryManagementSystem.Models;

public class PublicSearchViewModel
{
    public string? SearchTerm { get; set; }

    public string? ItemType { get; set; }

    public ItemStatus? Status { get; set; }

    public int? BranchId { get; set; }

    public List<Branch> Branches { get; set; } = new();

    public List<Item> Results { get; set; } = new();

    // Prevents all catalogue items appearing
    // before the user actually performs a search.
    public bool HasSearched { get; set; }
}