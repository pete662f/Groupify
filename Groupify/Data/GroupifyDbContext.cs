using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Groupify.Models.Identity;
using Groupify.Models.Domain;
namespace Groupify.Data;

public class GroupifyDbContext : IdentityDbContext<ApplicationUser>
{
    public GroupifyDbContext(DbContextOptions<GroupifyDbContext> options) : base(options) {}
    
    public DbSet<StudentProfile> StudentProfiles { get; set; } = null!;
    public DbSet<AdminProfile> AdminProfiles { get; set; } = null!;
    public DbSet<TeacherProfile> TeacherProfiles { get; set; } = null!;
    public DbSet<Insight> Insights { get; set; } = null!;
    public DbSet<Room> Rooms { get; set; } = null!;
    public DbSet<Group> Groups { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // 1:1 StudentProfile <-> ApplicationUser
        builder.Entity<ApplicationUser>()
            .HasOne(u => u.StudentProfile)
            .WithOne(p => p.User)
            .HasForeignKey<StudentProfile>(p => p.UserId);
        
        // 1:1 TeacherProfile <-> ApplicationUser
        builder.Entity<ApplicationUser>()
            .HasOne(u => u.TeacherProfile)
            .WithOne(p => p.User)
            .HasForeignKey<TeacherProfile>(p => p.UserId);
        
        // 1:1 AdminProfile <-> ApplicationUser
        builder.Entity<ApplicationUser>()
            .HasOne(u => u.AdminProfile)
            .WithOne(p => p.User)
            .HasForeignKey<AdminProfile>(p => p.UserId);
        
        // M:N StudentProfile <-> Room
        builder.Entity<Room>()
            .HasMany(r => r.Students)
            .WithMany(s => s.Rooms)
            .UsingEntity(j => j.ToTable("StudentRoom"));
        
        // 1:N TeacherProfile <-> Room
        builder.Entity<TeacherProfile>()
            .HasMany(t => t.CreatedRooms)
            .WithOne(r => r.Teacher)
            .HasForeignKey(r => r.TeacherProfileUserId);
        
        // 1:N Room <-> Group
        builder.Entity<Room>()
            .HasMany(r => r.Groups)
            .WithOne(g => g.Room)
            .HasForeignKey(g => g.RoomId);
    }
}