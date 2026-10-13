namespace LibraryManagementSystem.DTOs;

public class ItemSearchResultDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string LibraryCode { get; set; } = string.Empty;

    public string ItemType { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}