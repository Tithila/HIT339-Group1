namespace LibraryManagementSystem.Models;

public class PublicSearchViewModel
{
    public string? SearchTerm { get; set; }

    public string? ItemType { get; set; }

    public ItemStatus? Status { get; set; }

    public List<Item> Results { get; set; } = new();
}