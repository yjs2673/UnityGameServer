using StackExchange.Redis;

namespace MyGameServer.Services;

// 서버 시작 시 Redis에 저장된 로그인 세션 초기화 서비스
public class RedisInitService : IHostedService
{
    private readonly IConnectionMultiplexer _redis;     // Redis 연결 객체
    private readonly ILogger<RedisInitService> _logger; // 로거

    // 생성자
    public RedisInitService(IConnectionMultiplexer redis, ILogger<RedisInitService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    // 서버 시작 시 Redis에 저장된 로그인 세션 초기화
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("== [Redis] 서버 시작: 로그인 세션 초기화 중... ==");

        var endpoints = _redis.GetEndPoints();          // Redis 서버의 엔드포인트
        var server = _redis.GetServer(endpoints[0]);    // Redis 서버 객체

        // "login_status:*" 패턴을 가진 모든 키를 찾아 삭제
        var keys = server.Keys(pattern: "*login_status:*").ToArray();
        if (keys.Length > 0)
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(keys);
            _logger.LogInformation($"== [Redis] 초기화 완료: {keys.Length}개의 세션이 정리되었습니다. ==");
        }
        else
            _logger.LogInformation("== [Redis] 정리할 활성 세션이 없습니다. ==");
    }

    // 서버 종료 시 별도의 작업은 필요 X
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}