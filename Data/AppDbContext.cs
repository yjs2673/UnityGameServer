using Microsoft.EntityFrameworkCore;
using MyGameServer.Models;
using Microsoft.Extensions.DependencyInjection;

// DB 컨텍스트 클래스: Entity Framework Core를 사용하여 MySQL과 연결
public class AppDbContext : DbContext
{
    // API 서버 생성자
    [ActivatorUtilitiesConstructor]
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // 소켓 서버 생성자
    public AppDbContext() { }

    // DB 테이블 매핑
    public DbSet<User> Users { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<UserItem> UserItems { get; set; }

    // DB 연결 설정
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            string connStr = "Server=localhost;Database=GameDB;Uid=root;Pwd=2673;";
            var serverVersion = new MySqlServerVersion(new System.Version(8, 0, 45));
            optionsBuilder.UseMySql(connStr, serverVersion);
        }
    }
}