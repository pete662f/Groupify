using Groupify.Data;
using Groupify.Models.Domain;
using Groupify.Models.Identity;
using Groupify.Tests.Helpers;

namespace Groupify.Tests.Services;

public class GroupServiceTests
{
    [Fact]
    public async Task GroupInsightAsync_GroupExists_ReturnsCorrectAverage()
    {
        // Arrange
        var fakeGroup = new Group {Id = Guid.NewGuid()};
        fakeGroup.Users.Add(new ApplicationUser
        {
            Insight = new Insight { Red = 1, Green = 1, Blue = 1, Yellow = 1 }
        });
        fakeGroup.Users.Add(new ApplicationUser
        {
            Insight = new Insight { Red = 3, Green = 3, Blue = 3, Yellow = 3 }
        });
        
        //TODO: Find a way to mock the DbSet
        
        
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task CreateGroupsAsync_InvalidSize_Throws(int invalidSize)
    {
        // Arrange
        await using var db = InMemoryDbContextFactory.Create();
        var groupService = new GroupService(db);
        
        // Act
        var exception = await Record.ExceptionAsync(() =>
            groupService.CreateGroupsAsync(Guid.NewGuid(), invalidSize)
        );

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
    }
}