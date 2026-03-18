using System.ComponentModel.DataAnnotations;

namespace MyGameServer.Models;

public class User
{
    [Key] // 기본키(Primary Key) 설정
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string LoginId { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty; // 실제 비번 대신 암호화된 값을 저장

    [Required]
    [MaxLength(15)]
    public string Nickname { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // 계정 생성일
}