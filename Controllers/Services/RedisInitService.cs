using StackExchange.Redis;

namespace MyGameServer.Services;

public class RedisInitService : IHostedService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisInitService> _logger;

    public RedisInitService(IConnectionMultiplexer redis, ILogger<RedisInitService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("== [Redis] 서버 시작: 로그인 세션 초기화 중... ==");

        var endpoints = _redis.GetEndPoints();
        var server = _redis.GetServer(endpoints[0]);

        // "login_status:*" 패턴을 가진 모든 키를 찾아 삭제
        var keys = server.Keys(pattern: "*login_status:*").ToArray();
        
        if (keys.Length > 0)
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(keys);
            _logger.LogInformation($"== [Redis] 초기화 완료: {keys.Length}개의 세션이 정리되었습니다. ==");
        }
        else
        {
            _logger.LogInformation("== [Redis] 정리할 활성 세션이 없습니다. ==");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}