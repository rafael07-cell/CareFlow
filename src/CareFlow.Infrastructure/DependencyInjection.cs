using CareFlow.Application.Interfaces;
using CareFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CareFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.AddDbContextFactory<CareFlowDbContext>(options =>
            options.UseNpgsql(DatabasePaths.ConnectionString));

        services.AddSingleton<IBookingUnitOfWorkFactory, EfBookingUnitOfWorkFactory>();
        services.AddSingleton<IBookingQueries, EfBookingQueries>();

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider provider)
    {
        var factory = provider.GetRequiredService<IDbContextFactory<CareFlowDbContext>>();
        await DbInitializer.InitializeAsync(factory);
    }
}
