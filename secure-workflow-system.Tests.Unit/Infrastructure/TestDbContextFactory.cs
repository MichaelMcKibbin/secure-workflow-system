using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using secure_workflow_system.Data;

namespace secure_workflow_system.Tests.Unit.Infrastructure;

/// <summary>
/// Provides in-memory database context for unit tests
/// </summary>
public class TestDbContextFactory
{
    public static ApplicationDbContext CreateTestContext(string databaseName = "TestDb")
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        var context = new ApplicationDbContext(options);
        return context;
    }

    public static ApplicationDbContext CreateAndSeedTestContext(string databaseName = "TestDb")
    {
        var context = CreateTestContext(databaseName);
        context.Database.EnsureCreated();
        return context;
    }
}
