using Microsoft.EntityFrameworkCore;
using MyGameServer.Models;
using Microsoft.Extensions.DependencyInjection;

public class AppDbContext : DbContext
{
    // API 서버 생성자를 사용하도록 강제 지
    [ActivatorUtilitiesConstructor]
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    // 소켓 서버 생성자
    public AppDbContext() {}

    public DbSet<User> Users { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<UserItem> UserItems { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // DB 연결
            string connStr = "Server=localhost;Database=GameDB;Uid=root;Pwd=2673;";
            var serverVersion = new MySqlServerVersion(new System.Version(8, 0, 45));
            optionsBuilder.UseMySql(connStr, serverVersion);
        }
    }
}