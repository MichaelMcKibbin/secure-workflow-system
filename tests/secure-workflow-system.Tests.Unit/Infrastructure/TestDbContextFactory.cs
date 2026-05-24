using Microsoft.EntityFrameworkCore;
using secure_workflow_system.Data;

namespace secure_workflow_system.Tests.Unit.Infrastructure;

public static class TestDbContextFactory
{
    public static ApplicationDbContext Create(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
