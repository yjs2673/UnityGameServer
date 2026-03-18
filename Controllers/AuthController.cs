using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyGameServer.Models;

namespace MyGameServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    // 회원가입: POST /api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] User user)
    {
        // 1. 아이디 중복 체크
        if (await _context.Users.AnyAsync(u => u.LoginId == user.LoginId))
        {
            return BadRequest("이미 존재하는 아이디입니다.");
        }

        // 2. DB에 저장
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = "회원가입 성공!", userId = user.Id });
    }
}