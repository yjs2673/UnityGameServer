namespace MyGameServer.Models;

// 레벨 업데이트 요청 DTO
public class LevelUpdateDto
{
    public int Id { get; set; }
    public int AddedExp { get; set; }
}