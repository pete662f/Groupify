using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Groupify.Models.Identity;
using Groupify.Models.Domain;
namespace Groupify.Data;

public class GroupifyDbContext : IdentityDbContext<ApplicationUser>
{
    public GroupifyDbContext(DbContextOptions<GroupifyDbContext> options) : base(options) {}
    
    public DbSet<Insight> Insights { get; set; } = null!;
    public DbSet<Room> Rooms { get; set; } = null!;
    public DbSet<Group> Groups { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // M:N User <-> Room
        builder.Entity<Room>()
            .HasMany(r => r.Users)
            .WithMany(s => s.Rooms)
            .UsingEntity(j => j.ToTable("UserRoom"));
        
        // 1:N RoomOwner <-> Room
        builder.Entity<ApplicationUser>()
            .HasMany(t => t.CreatedRooms)
            .WithOne(r => r.Owner)
            .HasForeignKey(r => r.OwnerId);
        
        // 1:N Room <-> Group
        builder.Entity<Room>()
            .HasMany(r => r.Groups)
            .WithOne(g => g.Room)
            .HasForeignKey(g => g.RoomId);
    }
}