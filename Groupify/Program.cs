using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Groupify.Data;
using Groupify.Data.Services;
using Groupify.Models.Identity;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
[assembly: InternalsVisibleTo("Groupify.Testsl")] // For testing purposes
[assembly: InternalsVisibleTo("Groupify.IntegrationTests")] // For integration tests

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<GroupifyDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddScoped<RoomService>();
builder.Services.AddScoped<GroupService>();
builder.Services.AddScoped<InsightService>();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<GroupifyDbContext>();
builder.Services.AddControllersWithViews();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.WebHost.UseStaticWebAssets();

var app = builder.Build();

// Set up the database
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<GroupifyDbContext>();
db.Database.Migrate();

// Configure the roles
var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
foreach (var role in new[] { "Admin", "Teacher", "Student" })
{
    if (!await roleManager.RoleExistsAsync(role))
        await roleManager.CreateAsync(new IdentityRole(role));
}



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    // Static files are served from the wwwroot folder in production
    app.UseStaticFiles();
    StaticWebAssetsLoader.UseStaticWebAssets(app.Environment, app.Configuration);
    
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
    .WithStaticAssets();

// Seed data
System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
await SeedData.InitializeAsync(scope.ServiceProvider);

app.Run();