using Groupify.Data;
using Groupify.Data.Services;
using Groupify.Models.Domain;
using Groupify.IntegrationTests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Groupify.IntegrationTests.Services;

public class GroupServiceIntegrationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task CreateGroupsAsync_InvalidSize_Throws(int invalidSize)
    {
        // Arranges
        await using var db = SqliteInMemoryContextFactory.Create();
        
        // Disable foreign key checks for testing
        await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=OFF;"); 
        var groupService = new GroupService(db);

        // Seed a room so we get past the "room not found" check
        var room = new Room
        {
            Id = Guid.NewGuid(),
            Name = "Test Room",
            OwnerId = Guid.NewGuid().ToString()
        };
        db.Rooms.Add(room);
        await db.SaveChangesAsync();

        // Act
        var exception = await Record.ExceptionAsync(() =>
            groupService.CreateGroupsAsync(Guid.NewGuid(), invalidSize)
        );
        
        // Assert
        Assert.IsType<InvalidOperationException>(exception);
    }
}