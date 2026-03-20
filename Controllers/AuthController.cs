using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using MyGameServer.Models;

namespace MyGameServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IDatabase _redis;

    public AuthController(AppDbContext context, IConnectionMultiplexer redis)
    {
        _context = context;
        _redis = redis.GetDatabase();
    }

    // 회원가입: POST /api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerinfo)
    {
        // 아이디, 닉네임 글자수 체크
        if (!ModelState.IsValid)
        {
            // 첫 번째로 발견된 에러 메시지만 골라서 반환합니다.
            var errorMessage = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault();
            return BadRequest(errorMessage);
        }

        // 아이디 중복 체크
        if (await _context.Users.AnyAsync(u => u.LoginId == registerinfo.LoginId))
            return BadRequest("이미 존재하는 아이디입니다.");

        // 닉네임 중복 체크
        if (await _context.Users.AnyAsync(u => u.Nickname == registerinfo.Nickname))
            return BadRequest("이미 존재하는 닉네임입니다.");    

       // DTO 데이터를 실제 User 엔티티로 변환
        var newUser = new User
        {
            LoginId = registerinfo.LoginId,
            Password = BCrypt.Net.BCrypt.HashPassword(registerinfo.Password), // 암호화 적용
            Nickname = registerinfo.Nickname,
            Level = 1,
            Gold = 0,
            Exp = 0
        };

        // DB 저장
        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        return Ok(new { message = "회원가입 성공!" });
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
        // Redis Sorted Set에 업데이트
        await _redis.SortedSetAddAsync("user_ranking", user.Id.ToString(), user.Level);

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
        // Redis에서 상위 5명 가져오기 (내림차순)
        var redisResults = await _redis.SortedSetRangeByRankWithScoresAsync("user_ranking", 0, 4, Order.Descending);

        if (redisResults.Length == 0) return Ok(new List<RankData>());

        var userIds = redisResults.Select(r => int.Parse(r.Element.ToString())).ToList();

        var userDict = await _context.Users
        .Where(u => userIds.Contains(u.Id))
        .ToDictionaryAsync(u => u.Id, u => u.Nickname);

        // Redis 순서(점수 순)를 유지하며 RankData 리스트 생성
        var rankingList = redisResults.Select((entry, index) => {
            int id = int.Parse(entry.Element.ToString());
            return new RankData {
                id = id, // 유저 고유 ID
                nickname = userDict.ContainsKey(id) ? userDict[id] : "Unknown", // DB에서 찾은 닉네임
                level = (int)entry.Score
            };
        }).ToList();

    return Ok(rankingList);
    }
}