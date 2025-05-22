using Groupify.Data;
using Groupify.Data.Services;
using Groupify.IntegrationTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Groupify.IntegrationTests.Services;
public class GroupServiceSeededIntegrationTests 
    : IClassFixture<IntegrationTestsFixture>
{
    private readonly GroupifyDbContext _context;
    private readonly GroupService      _service;

    public GroupServiceSeededIntegrationTests(IntegrationTestsFixture fixture)
    {
        var scope = fixture.ServiceProvider.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<GroupifyDbContext>();
        _service = new GroupService(_context);
    }

    [Fact]
    public async Task MegaRoom_IsSeededWithThousandStudents()
    {
        var mega = await _context.Rooms
                                 .Include(r => r.Users)
                                 .FirstOrDefaultAsync(r => r.Name == "Mega Room");
        Assert.NotNull(mega);
        Assert.Equal(1000, mega.Users.Count);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(10)]
    [InlineData(50)]
    public async Task CreateGroupsAsync_OnMegaRoom_SplitsCorrectly(int groupSize)
    {
        // Arrange
        var mega = await _context.Rooms
                                 .Include(r => r.Users)
                                 .FirstAsync(r => r.Name == "Mega Room");

        // Remove any existing groups so we can re-create
        await _service.RemoveAllGroupsByRoomIdAsync(mega.Id);

        int expected = (int)Math.Ceiling(1000.0 / groupSize);

        // Act
        await _service.CreateGroupsAsync(mega.Id, groupSize);

        // Assert
        var groups = await _context.Groups
                                   .Include(g => g.Users)
                                   .Where(g => g.RoomId == mega.Id)
                                   .ToListAsync();

        Assert.Equal(expected, groups.Count);
        Assert.All(groups, g =>
            Assert.InRange(g.Users.Count, 1, groupSize));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task CreateGroupsAsync_InvalidSize_ThrowsArgument(int invalidSize)
    {
        var mega = await _context.Rooms
                                 .FirstAsync(r => r.Name == "Mega Room");

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateGroupsAsync(mega.Id, invalidSize)
        );
    }

    [Fact]
    public async Task CreateGroupsAsync_TooFewUsersInTeacherRoom_Throws()
    {
        var teacher0 = await _context.Users.FirstAsync(u => u.Email == "teacher0@demo.com");
        var room60   = await _context.Rooms
                                     .Include(r => r.Users)
                                     .FirstAsync(r => r.OwnerId == teacher0.Id);

        // groupSize = 60 → needs at least 62 users
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateGroupsAsync(room60.Id, 60)
        );
    }

    [Theory]
    [InlineData(10)]
    [InlineData(15)]
    public async Task CreateGroupsAsync_OnTeacherRoom_SplitsCorrectly(int groupSize)
    {
        var teacher1 = await _context.Users.FirstAsync(u => u.Email == "teacher1@demo.com");
        var room     = await _context.Rooms
                                     .Include(r => r.Users)
                                     .FirstAsync(r => r.OwnerId == teacher1.Id);

        // clear any prior groups
        await _service.RemoveAllGroupsByRoomIdAsync(room.Id);

        int expected = (int)Math.Ceiling(room.Users.Count / (double)groupSize);

        await _service.CreateGroupsAsync(room.Id, groupSize);

        var groups = await _context.Groups
                                   .Include(g => g.Users)
                                   .Where(g => g.RoomId == room.Id)
                                   .ToListAsync();

        Assert.Equal(expected, groups.Count);
        Assert.All(groups, g =>
            Assert.InRange(g.Users.Count, 1, groupSize));
    }

    [Fact]
    public async Task GetGroupsByUserIdAsync_ReturnsExactlyOneUniqueGroup_ForBulkStudent()
    {
        // Arrange: ensure mega room has fresh groups
        var mega = await _context.Rooms.FirstAsync(r => r.Name == "Mega Room");
        await _service.RemoveAllGroupsByRoomIdAsync(mega.Id);
        await _service.CreateGroupsAsync(mega.Id, 25);

        // Act: pick a known bulk student
        var student = await _context.Users
            .FirstAsync(u => u.Email == "bulkstudent0@demo.com");
        var groups  = (await _service.GetGroupsByUserIdAsync(student.Id)).ToList();

        // Assert
        Assert.NotEmpty(groups);

        // Ensure exactly one *unique* group Id
        var distinctIds = groups.Select(g => g.Id).Distinct().ToList();
        Assert.Single(distinctIds);

        // And that group's RoomId is the Mega Room
        var theGroup = groups.First(g => g.Id == distinctIds.Single());
        Assert.Equal(mega.Id, theGroup.RoomId);
    }

    [Fact]
    public async Task GetGroupByUserIdAndRoomIdAsync_ReturnsEmpty_WhenNotAssigned()
    {
        var teacher = await _context.Users.FirstAsync(u => u.Email == "teacher0@demo.com");
        var room    = await _context.Rooms.FirstAsync(r => r.OwnerId == teacher.Id);

        var result = await _service.GetGroupByUserIdAndRoomIdAsync(teacher.Id, room.Id);
        Assert.Equal(Guid.Empty, result);
    }
}