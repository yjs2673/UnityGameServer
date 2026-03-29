using System.ComponentModel.DataAnnotations.Schema;

// 유저가 보유한 아이템 정보 모델
public class UserItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ItemId { get; set; }
    public int Count { get; set; }


    [ForeignKey("ItemId")] // EF Core가 조인을 수행할 때 참조할 외래 키 객체
    public virtual Item? Item { get; set; } // 네비게이션 속성 (조인 시 사용)
}