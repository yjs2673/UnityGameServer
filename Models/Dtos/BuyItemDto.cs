namespace MyGameServer.Models;

// 아이템 구매 요청 DTO
public class BuyItemDto
{
    public int UserId { get; set; }
    public int ItemId { get; set; }
    public int Count { get; set; } = 1;
}