namespace MyGameServer.Models;

public class InventoryItemDto
{
    public int ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Count { get; set; }
    public int ItemType { get; set; }
    public int AbilityValue { get; set; }
}