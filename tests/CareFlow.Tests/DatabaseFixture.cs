using CareFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.Tests;

public sealed class TestDbContextFactory : IDbContextFactory<CareFlowDbContext>
{
    private readonly DbContextOptions<CareFlowDbContext> _options;
    public TestDbContextFactory(DbContextOptions<CareFlowDbContext> options) => _options = options;
    public CareFlowDbContext CreateDbContext() => new(_options);
}

/// <summary>Creates a fresh PostgreSQL test database from the real EF Core migrations.</summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    public TestDbContextFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var password = Environment.GetEnvironmentVariable("CAREFLOW_DB_PASSWORD")
            ?? throw new InvalidOperationException("Set the CAREFLOW_DB_PASSWORD environment variable before running tests.");

        var cs = $"Host=localhost;Port=5432;Database=careflow_test;Username=postgres;Password={password}";
        var options = new DbContextOptionsBuilder<CareFlowDbContext>().UseNpgsql(cs).Options;
        Factory = new TestDbContextFactory(options);

        await using (var db = Factory.CreateDbContext())
            await db.Database.EnsureDeletedAsync();

        // Applies the real migrations and seeds the fictional patients and services.
        await DbInitializer.InitializeAsync(Factory);
    }

    public async Task ResetBookingsAsync()
    {
        await using var db = Factory.CreateDbContext();
        await db.BookingItems.ExecuteDeleteAsync();
        await db.Bookings.ExecuteDeleteAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }
