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
        // 아이디 중복 체크
        if (await _context.Users.AnyAsync(u => u.LoginId == user.LoginId))
            return BadRequest("이미 존재하는 아이디입니다.");

        // BCrypt를 이용한 비밀번호 해싱 암호화
        user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

        // DB 저장
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = "회원가입 성공!", userId = user.Id });
    }

    // 로그인: POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginInfo)
    {
        // 아이디 확인
        var user = await _context.Users.FirstOrDefaultAsync(u => u.LoginId == loginInfo.LoginId);

        if (user == null) return BadRequest("아이디가 존재하지 않습니다.");

        // 비밀번호 확인 -> 해시값 비교
        if (!BCrypt.Net.BCrypt.Verify(loginInfo.Password, user.Password))
            return BadRequest("비밀번호가 틀렸습니다.");

        return Ok(new
        {
            message = "로그인 성공!",
            userId = user.Id,
            nickname = user.Nickname,
            level = user.Level,
            exp = user.Exp,
            gold = user.Gold
        });
    }

    // 골드 업데이트: POST /api/auth/add-gold
    [HttpPost("add-gold")]
    public async Task<IActionResult> AddGold([FromBody] GoldUpdateDto info)
    {
        var user = await _context.Users.FindAsync(info.Id);
        if (user == null) return BadRequest("존재하지 않는 유저입니다.");

        // 골드 추가
        user.Gold += info.AddedGold;

        // 변경사항 저장
        await _context.SaveChangesAsync();

        return Ok(new
        {
            gold = user.Gold,
            message = "골드 업데이트 성공"
        });
    }

    // 경험치 업데이트: POST /api/auth/add-exp
    [HttpPost("add-exp")]
    public async Task<IActionResult> AddExp([FromBody] LevelUpdateDto info)
    {
        var user = await _context.Users.FindAsync(info.Id);
        if (user == null) return BadRequest("존재하지 않는 유저입니다.");

        // 경험치 추가
        user.Exp += info.AddedExp;

        // 경험치 100마다 레벨업
        while (user.Exp >= 100)
        {
            user.Exp -= 100;
            user.Level += 1;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            level = user.Level,
            exp = user.Exp,
            message = "경험치 획득 완료!"
        });
    }

    // 랭킹 조회: GET /api/auth/ranking
    [HttpGet("ranking")]
    public async Task<IActionResult> GetRanking()
    {
        var rankingList = await _context.Users
            .OrderByDescending(u => u.Level)    // 레벨 내림차순
            .ThenByDescending(u => u.Exp)       // 레벨이 같다면 경험치 높은 순
            .Take(5)                            // 상위 5유저
            .Select(u => new {                  // 보안을 위해 필요한 정보만 추출
                u.Id,
                u.Nickname,
                u.Level,
            })
            .ToListAsync();

        return Ok(rankingList);
    }
}