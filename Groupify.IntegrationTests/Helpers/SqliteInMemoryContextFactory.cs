using Groupify.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Groupify.IntegrationTests.Helpers;

public static class SqliteInMemoryContextFactory
{
    public static GroupifyDbContext Create()
    {
        // 1) Create and open a SQLite in-memory connection
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        // 2) Configure DbContext to use SQLite on this connection
        var options = new DbContextOptionsBuilder<GroupifyDbContext>()
            .UseSqlite(connection)
            .Options;

        // 3) Instantiate context and apply the schema
        var context = new GroupifyDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }
}