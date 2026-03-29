using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyGameServer.Models;

namespace MyGameServer.Controllers;

// 상점 관련 API 컨트롤러
[ApiController]
[Route("api/[controller]")]
public class ShopController : ControllerBase
{
    private readonly AppDbContext _context; // DB 컨텍스트

    // 생성자
    public ShopController(AppDbContext context)
    {
        _context = context;
    }

    // 상점 아이템 목록 조회: GET /api/shop/items
    [HttpGet("items")]
    public async Task<IActionResult> GetShopItems()
    {
        // DB에서 모든 아이템 정보 조회
        var items = await _context.Items.ToListAsync();

        return Ok(items);
    }

    // 유저 인벤토리 조회: GET /api/shop/inventory/{userId}
    [HttpGet("inventory/{userId}")]
    public async Task<IActionResult> GetInventory(int userId)
    {
        var inventory = await _context.UserItems    // DB UserItems 테이블에서
            .Where(ui => ui.UserId == userId)       // 해당 유저의 아이템만 필터링
            .Include(ui => ui.Item)                 // Item 마스터 정보 조인
            .Select(ui => new InventoryItemDto      // DTO로 변환
            {
                ItemId = ui.ItemId,
                Name = ui.Item!.Name,
                Description = ui.Item.Description,
                Count = ui.Count,
                ItemType = ui.Item.ItemType,
                AbilityValue = ui.Item.AbilityValue
            })
            .ToListAsync(); // 결과 리스트로 반환

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

            if (user == null)
                return BadRequest("유저 정보가 존재하지 않습니다.");
            if (item == null)
                return BadRequest("아이템 정보가 존재하지 않습니다.");

            // 골드 잔액 체크
            int totalCost = item.Price * dto.Count;
            if (user.Gold < totalCost)
                return BadRequest("골드가 부족합니다.");

            // 골드 차감
            user.Gold -= totalCost;

            // 인벤토리 업데이트
            var userItem = await _context.UserItems
                .FirstOrDefaultAsync(ui => ui.UserId == dto.UserId && ui.ItemId == dto.ItemId);

            if (userItem != null)   // 이미 아이템이 존재하면 수량 증가
                userItem.Count += dto.Count;
            else                    // 존재하지 않으면 새 아이템 추가
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

            return Ok(new { message = $"{item.Name} 구매 완료!", currentGold = user.Gold });
        }
        catch (Exception ex)
        {
            // 오류 발생 시 모든 변경사항 롤백
            await transaction.RollbackAsync();
            return StatusCode(500, $"서버 오류 발생: {ex.Message}");
        }
    }
}