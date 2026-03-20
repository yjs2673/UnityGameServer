using System.ComponentModel.DataAnnotations;

namespace MyGameServer.Models;

public class RegisterDto
{
    [Required]
    [StringLength(10, ErrorMessage = "아이디는 10자 이하로 입력해주세요.")]
    public string LoginId { get; set; } = string.Empty;

    [Required]
    [StringLength(10, ErrorMessage = "닉네임은 10자 이하로 입력해주세요.")]
    public string Nickname { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}