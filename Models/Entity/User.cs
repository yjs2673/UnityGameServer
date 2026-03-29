using System.ComponentModel.DataAnnotations;

namespace MyGameServer.Models;

// 유저 정보 모델
public class User
{
    [Key] // 기본키(Primary Key) 설정
    public int Id { get; set; }                                 // 아이디

    [Required]
    [MaxLength(10)]
    public string LoginId { get; set; } = string.Empty;         // 비밀번호

    [Required]
    public string Password { get; set; } = string.Empty;        // 닉네임

    [Required]
    [MaxLength(10)]
    public string Nickname { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // 계정 생성일

    public int Level { get; set; } = 1;     // 레벨
    public int Exp { get; set; } = 0;       // 경험치
    public int Gold { get; set; } = 0;      // 골드
}