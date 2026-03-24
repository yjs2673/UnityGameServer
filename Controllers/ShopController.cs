using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyGameServer.Models;

namespace MyGameServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShopController : ControllerBase
{
    private readonly AppDbContext _context;

    public ShopController(AppDbContext context)
    {
        _context = context;
    }

    // 상점 아이템 목록 조회: GET /api/shop/items
    [HttpGet("items")]
    public async Task<IActionResult> GetShopItems()
    {
        var items = await _context.Items.ToListAsync();
        return Ok(items);
    }

    // 유저 인벤토리 조회: GET /api/shop/inventory/{userId}
    [HttpGet("inventory/{userId}")]
    public async Task<IActionResult> GetInventory(int userId)
    {
        var inventory = await _context.UserItems
            .Where(ui => ui.UserId == userId)
            .Include(ui => ui.Item) // Item 마스터 정보 조인
            .Select(ui => new InventoryItemDto
            {
                ItemId = ui.ItemId,
                Name = ui.Item!.Name,
                Description = ui.Item.Description,
                Count = ui.Count,
                ItemType = ui.Item.ItemType,
                AbilityValue = ui.Item.AbilityValue
            })
            .ToListAsync();

        return Ok(inventory);
    }

    // 아이템 구매: POST /api/shop/buy
    [HttpPost("buy")]
    public async Task<IActionResult> BuyItem([FromBody] BuyItemDto dto)
    {
        // 원자성(Atomicity) 보장을 위한 트랜잭션 시작
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 유저 및 아이템 정보 조회
            var user = await _context.Users.FindAsync(dto.UserId);
            var item = await _context.Items.FindAsync(dto.ItemId);

            if (user == null || item == null)
                return BadRequest("유저 또는 아이템 정보가 존재하지 않습니다.");

            // 골드 잔액 체크
            int totalCost = item.Price * dto.Count;
            if (user.Gold < totalCost) return BadRequest("골드가 부족합니다.");

            user.Gold -= totalCost;

            // 인벤토리 업데이트 (이미 있으면 수량 증가, 없으면 새로 추가)
            var userItem = await _context.UserItems
                .FirstOrDefaultAsync(ui => ui.UserId == dto.UserId && ui.ItemId == dto.ItemId);

            if (userItem != null) userItem.Count += dto.Count;
            else
            {
                _context.UserItems.Add(new UserItem
                {
                    UserId = dto.UserId,
                    ItemId = dto.ItemId,
                    Count = dto.Count
                });
            }

            // DB 반영
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new 
            { 
                message = $"{item.Name} 구매 완료!", 
                currentGold = user.Gold 
            });
        }
        catch (Exception ex)
        {
            // 오류 발생 시 모든 변경사항 롤백
            await transaction.RollbackAsync();
            return StatusCode(500, $"서버 오류 발생: {ex.Message}");
        }
    }
}