using Groupify.Data;
using Groupify.Data.Services;
using Groupify.IntegrationTests.Helpers;
using Groupify.Models.Domain;
using Groupify.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Groupify.IntegrationTests.Services;
public class InsightServiceIntegrationTests : IClassFixture<IntegrationTestsFixture>
{
    private readonly IServiceProvider _provider;
    private readonly GroupifyDbContext _context;
    private readonly InsightService _service;

    public InsightServiceIntegrationTests(IntegrationTestsFixture fixture)
    {
        _provider = fixture.ServiceProvider;
        var scope = _provider.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<GroupifyDbContext>();
        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        _service = new InsightService(_context, userMgr);
    }

    [Theory]
    [InlineData("bulkstudent0@demo.com", true)]
    [InlineData("teacher0@demo.com", false)]
    [InlineData("does-not-exist@example.com", false)]
    public async Task HasInsightProfile_WorksAsExpected(string email, bool expected)
    {
        // if it’s a real seeded email, look it up; otherwise pick a random GUID
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
        string userId = user?.Id ?? Guid.NewGuid().ToString();

        var result = await _service.HasInsightProfileAsync(userId);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetInsightByUserIdAsync_ReturnsInsight_ForSeededStudent()
    {
        var student = await _context.Users
            .FirstAsync(u => u.Email == "bulkstudent0@demo.com");

        var insight = await _service.GetInsightByUserIdAsync(student.Id);

        Assert.Equal(student.Id, insight.ApplicationUserId);
    }

    [Fact]
    public async Task GetInsightByUserIdAsync_ThrowsNotFound_ForTeacher()
    {
        var teacher = await _context.Users
            .FirstAsync(u => u.Email == "teacher0@demo.com");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetInsightByUserIdAsync(teacher.Id)
        );
        Assert.Equal("Insight not found", ex.Message);
    }

    [Fact]
    public async Task GetInsightByUserIdAsync_ThrowsUserNotFound_ForUnknownUser()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetInsightByUserIdAsync("no-such-id")
        );
        Assert.Equal("User not found", ex.Message);
    }

    [Fact]
    public async Task CreateInsightProfileAsync_CreatesProfile_ForNewStudent()
    {
        using var scope = _provider.CreateScope();
        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Create a new test student
        var user = new ApplicationUser
        {
            UserName       = "newstudent@demo.test",
            Email          = "newstudent@demo.test",
            EmailConfirmed = true,
            FirstName      = "New",
            LastName       = "Student"
        };
        var createRes = await userMgr.CreateAsync(user, "P@ssw0rd!");
        Assert.True(createRes.Succeeded, "UserManager.CreateAsync failed");

        await userMgr.AddToRoleAsync(user, "Student");

        // Invoke service
        var newInsight = new Insight { Red = 1, Blue = 2, Green = 3, Yellow = 4, WheelPosition = 5 };
        await _service.CreateInsightProfileAsync(user.Id, newInsight);

        // Verify in DB
        var ins = await _context.Insights.FindAsync(user.Id);
        Assert.NotNull(ins);
        Assert.Equal(1, ins.Red);
        Assert.Equal(2, ins.Blue);
        Assert.Equal(3, ins.Green);
        Assert.Equal(4, ins.Yellow);
        Assert.Equal(5, ins.WheelPosition);
        Assert.Equal(user.Id, ins.ApplicationUserId);
    }

    [Fact]
    public async Task CreateInsightProfileAsync_ThrowsUserNotFound()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateInsightProfileAsync("does-not-exist", new Insight())
        );
        Assert.Equal("User not found", ex.Message);
    }

    [Fact]
    public async Task CreateInsightProfileAsync_ThrowsNonStudent()
    {
        // Pick a teacher
        var teacher = await _context.Users
            .FirstAsync(u => u.Email == "teacher0@demo.com");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateInsightProfileAsync(teacher.Id, new Insight())
        );
        Assert.Equal("User is not a student", ex.Message);
    }

    [Fact]
    public async Task CreateInsightProfileAsync_ThrowsAlreadyExists()
    {
        // Pick a bulk student (already has insight via seed)
        var student = await _context.Users
            .FirstAsync(u => u.Email == "bulkstudent0@demo.com");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateInsightProfileAsync(student.Id, new Insight())
        );
        Assert.Equal("User is already an existing insight", ex.Message);
    }

    [Fact]
    public async Task UpdateInsightAsync_UpdatesValues()
    {
        // Create a fresh student + profile so we don't mutate seeded
        using var scope = _provider.CreateScope();
        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName       = "updatetest@demo.test",
            Email          = "updatetest@demo.test",
            EmailConfirmed = true,
            FirstName      = "Update",
            LastName       = "Test"
        };
        Assert.True((await userMgr.CreateAsync(user, "P@ssw0rd!")).Succeeded);
        await userMgr.AddToRoleAsync(user, "Student");
        await _service.CreateInsightProfileAsync(user.Id, new Insight { Red=1,Blue=1,Green=1,Yellow=1,WheelPosition=1 });

        // Update
        var updated = new Insight { Red=9, Blue=8, Green=7, Yellow=6, WheelPosition=5 };
        await _service.UpdateInsightAsync(user.Id, updated);

        // Verify
        var ins = await _context.Insights.FindAsync(user.Id);
        Assert.Equal(9, ins?.Red);
        Assert.Equal(8, ins?.Blue);
        Assert.Equal(7, ins?.Green);
        Assert.Equal(6, ins?.Yellow);
        Assert.Equal(5, ins?.WheelPosition);
    }

    [Fact]
    public async Task UpdateInsightAsync_ThrowsNoProfile()
    {
        // Pick a teacher who has no insight
        var teacher = await _context.Users
            .FirstAsync(u => u.Email == "teacher0@demo.com");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateInsightAsync(teacher.Id, new Insight())
        );
        Assert.Equal("User does not have an insight profile", ex.Message);
    }

    [Fact]
    public async Task UpdateInsightAsync_ThrowsUserNotFound()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateInsightAsync("nonexistent", new Insight())
        );
        Assert.Equal("User not found", ex.Message);
    }
}
