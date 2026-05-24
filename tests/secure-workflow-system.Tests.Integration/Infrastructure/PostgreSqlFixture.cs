using Microsoft.EntityFrameworkCore;
using secure_workflow_system.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace secure_workflow_system.Tests.Integration.Infrastructure;

/// <summary>
/// Spins up a real PostgreSQL container once per test collection,
/// applies EF Core migrations, and provides a factory for per-test DbContext instances.
/// </summary>
public class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("test_db")
        .WithUsername("test_user")
        .WithPassword("test_pass")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Apply migrations to the real database
        var options = BuildOptions();
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    /// <summary>
    /// Creates a fresh DbContext for each test. Each test should dispose it.
    /// </summary>
    public ApplicationDbContext CreateContext() => new(BuildOptions());

    private DbContextOptions<ApplicationDbContext> BuildOptions() =>
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
}

[CollectionDefinition("PostgreSQL Collection")]
public class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture> { }
