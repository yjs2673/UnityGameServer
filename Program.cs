using Microsoft.EntityFrameworkCore;
using MyGameServer.Models;

var builder = WebApplication.CreateBuilder(args);

// DB 연결
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// MySQL 등록 (Pomelo)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddControllers();

var app = builder.Build();

// app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();