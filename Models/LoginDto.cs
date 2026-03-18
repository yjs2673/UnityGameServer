/* Data Transfer Object */
namespace MyGameServer.Models;

public class LoginDto
{
    public string LoginId { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}