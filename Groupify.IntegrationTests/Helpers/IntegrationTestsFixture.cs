using System.Text;
using Groupify.Data;
using Groupify.Models.Identity;
using Groupify.Data.Services.Interfaces;
using Groupify.Data.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Groupify.IntegrationTests.Helpers;
public class IntegrationTestsFixture : IDisposable
{
    private readonly SqliteConnection _connection;
    public IServiceProvider ServiceProvider { get; }

    public IntegrationTestsFixture()
    {
        // For ExcelDataReader
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // Open a single in-memory SQLite connection
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Build the service collection exactly as in Program.cs
        var services = new ServiceCollection();

        services.AddLogging(cfg => cfg.AddConsole());
        
        services.AddDbContext<GroupifyDbContext>(opts =>
            opts.UseSqlite(
                _connection,
                sql => sql.MigrationsAssembly(typeof(Program).Assembly.GetName().Name)
            )
        );

        // Mirror real identity setup
        services.AddDefaultIdentity<ApplicationUser>(opts =>
            opts.SignIn.RequireConfirmedAccount = true
        )
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<GroupifyDbContext>();

        // Application services
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<IInsightService, InsightService>();

        ServiceProvider = services.BuildServiceProvider();

        // MUTATE the database: apply migrations + create roles + seed
        using var scope = ServiceProvider.CreateScope();

        var ctx = scope.ServiceProvider.GetRequiredService<GroupifyDbContext>();
        
        // This is the same connection, so we only open it once
        ctx.Database.Migrate();

        // Create the three roles
        var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Admin", "Teacher", "Student" })
        {
            if (!roleMgr.RoleExistsAsync(role).GetAwaiter().GetResult())
                roleMgr.CreateAsync(new IdentityRole(role)).GetAwaiter().GetResult();
        }
        
        SeedData.InitializeAsync(scope.ServiceProvider)
                .GetAwaiter()
                .GetResult();
    }

    public void Dispose()
    {
        // Dispose the root provider and close the shared connection
        (ServiceProvider as IDisposable)?.Dispose();
        _connection.Close();
        _connection.Dispose();
    }
}
