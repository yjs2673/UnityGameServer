using Microsoft.EntityFrameworkCore;
using MyGameServer.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // DB 테이블 매핑
    public DbSet<User> Users { get; set; }          // 유저 테이블
    public DbSet<Item> Items { get; set; }          // 아이템 테이블
    public DbSet<UserItem> UserItems { get; set; }  // 유저 아이템 테이블
}