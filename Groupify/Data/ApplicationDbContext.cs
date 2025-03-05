using Groupify.Data.Entities;
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

    DbSet<UserAccount> userAccounts;
    public DbSet<Student> Students { get; set; }
    public DbSet<Employee> Employees { get; set; }

}
