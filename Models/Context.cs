using Microsoft.EntityFrameworkCore;

namespace todo.Models;

public class MyDbContext : DbContext
{
    public DbSet<User> users { get; set; }
    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //ability to change column type
        // modelBuilder.Entity<User>(e => e.Property(o => o.Username).HasColumnType(""));
    }
}