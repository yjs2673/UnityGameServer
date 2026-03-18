using Microsoft.EntityFrameworkCore;
using MyGameServer.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // 유저, 아이템 등 테이블 정보 추가

    // 유저 테이블 정의 (DB의 Users 테이블과 매핑됨)
    public DbSet<User> Users { get; set; }
}