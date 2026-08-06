using demo1.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace demo1.Tests.Helpers
{
    public static class DbContextTestHelper
    {
        public static AppDbContext CreateSqliteInMemoryDbContext()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new AppDbContext(options);
            context.Database.EnsureCreated(); // Creates tables from EF entities

            return context;
        }
    }
}
