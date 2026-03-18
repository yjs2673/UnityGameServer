using Microsoft.EntityFrameworkCore;
using MyGameServer.Models; // 모델 위치에 맞춰 수정 (예: MyGameServer.Models)

var builder = WebApplication.CreateBuilder(args);

// 1. DB 연결 문자열 가져오기
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 2. MySQL 서비스 등록 (Pomelo 라이브러리 함수 사용)
// 에러 원인: ServerVersion을 쓰려면 별도의 인스턴스가 필요하거나 명시적 지정이 필요함
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddControllers();

var app = builder.Build();

// // 3. HTTP 요청 파이프라인 설정
// if (app.Environment.IsDevelopment())
// {
//     app.MapOpenApi(); // .NET 10.0 기본 API 문서화 도구
// }

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();