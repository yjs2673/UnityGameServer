namespace MyGameServer.Models;

public class BuyItemDto
{
    public int UserId { get; set; }
    public int ItemId { get; set; }
    public int Count { get; set; } = 1;
}