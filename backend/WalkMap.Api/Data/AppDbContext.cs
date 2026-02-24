using Microsoft.EntityFrameworkCore;
using WalkMap.Api.Models;

namespace WalkMap.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Walk> Walks => Set<Walk>();
    public DbSet<WalkPoint> WalkPoints => Set<WalkPoint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.Username).IsUnique();
        });

        // Walk -> User (one-to-many)
        modelBuilder.Entity<Walk>(entity =>
        {
            entity.HasOne(w => w.User)
                  .WithMany(u => u.Walks)
                  .HasForeignKey(w => w.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // WalkPoint -> Walk (one-to-many)
        modelBuilder.Entity<WalkPoint>(entity =>
        {
            entity.HasOne(wp => wp.Walk)
                  .WithMany(w => w.RoutePoints)
                  .HasForeignKey(wp => wp.WalkId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(wp => new { wp.WalkId, wp.SequenceOrder });
        });
    }
}