using System.Text.RegularExpressions;
using Groupify.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Groupify.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<StudentUser> Students { get; set; }
    public DbSet<TeacherUser> Teachers { get; set; }
    public DbSet<InsightModel> Insights { get; set; }
    
}