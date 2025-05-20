using Groupify.Data;
using Microsoft.EntityFrameworkCore;

namespace Groupify.Tests.Helpers;

public static class InMemoryDbContextFactory
{
    public static GroupifyDbContext Create()
    {
        var options = new DbContextOptionsBuilder<GroupifyDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;
        
        var context = new GroupifyDbContext(options);
        
        // Seed the database with test data
        context.Database.EnsureCreated();
        
        return context;
    }
}