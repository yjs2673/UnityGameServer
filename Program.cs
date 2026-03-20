using Microsoft.EntityFrameworkCore;
using MyGameServer.Models;
using StackExchange.Redis;
using Microsoft.AspNetCore.Mvc;
using MyGameServer;

var builder = WebApplication.CreateBuilder(args);

// DB 연결
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// MySQL 등록 (Pomelo)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddControllers();

// Redis 연결 설정 추가
var redis = ConnectionMultiplexer.Connect("localhost");
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true; // 자동 에러 응답 비활성화
});

var chatServer = new ChatServer();
_ = Task.Run(() => chatServer.Start()); // 별도 테스크로 소켓 서버 실행

var app = builder.Build();
// app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();