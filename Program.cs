using Microsoft.EntityFrameworkCore;
using MyGameServer.Models;
using StackExchange.Redis;
using Microsoft.AspNetCore.Mvc;
using MyGameServer;
using MyGameServer.Services;
using Microsoft.Extensions.Caching.StackExchangeRedis;

var builder = WebApplication.CreateBuilder(args);

// DB 연결 설정
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddControllers();

// Redis 분산 캐시(IDistributedCache) 등록
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
    options.InstanceName = "MyGame_";
});

// Redis 연결 객체(IConnectionMultiplexer) 등록
var redis = ConnectionMultiplexer.Connect("localhost:6379");
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

// API 유효성 검사 커스텀 설정
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// ChatServer 싱글톤 등록
builder.Services.AddSingleton<ChatServer>();

// ParkServer 싱글톤 등록
builder.Services.AddSingleton<ParkServer>();

// 서버 시작 시 Redis 세션을 정리하는 Hosted Service 등록
builder.Services.AddHostedService<RedisInitService>();

var app = builder.Build();

// 채팅 서버 인스턴스 가져오기 및 시작
var chatServer = app.Services.GetRequiredService<ChatServer>();
_ = chatServer.Start(); 

// 공원 서버 인스턴스 가져오기 및 시작
var parkServer = app.Services.GetRequiredService<ParkServer>();
parkServer.Start();

app.UseAuthorization();
app.MapControllers();

app.Run();