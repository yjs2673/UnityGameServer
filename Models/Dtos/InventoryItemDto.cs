namespace MyGameServer.Models;

// 유저 인벤토리 아이템 정보 DTO
public class InventoryItemDto
{
    public int ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Count { get; set; }
    public int ItemType { get; set; }
    public int AbilityValue { get; set; }
}