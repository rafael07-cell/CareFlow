using CareFlow.Domain;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InitializeAsync(IDbContextFactory<CareFlowDbContext> factory)
    {
        await using var context = await factory.CreateDbContextAsync();

        // Applies pending migrations only; never recreates the database.
        await context.Database.MigrateAsync();

        // Seed only when empty. All data is fictional.
        if (await context.Patients.AnyAsync() || await context.DiagnosticServices.AnyAsync())
            return;

        context.Patients.AddRange(
            new Patient { FullName = "Ana Reyes" },
            new Patient { FullName = "Ben Cruz" },
            new Patient { FullName = "Carla Santos" },
            new Patient { FullName = "Diego Lim" });

        // Small capacities so a "fully booked" demo is quick.
        context.DiagnosticServices.AddRange(
            new DiagnosticService { Name = "Complete Blood Count", DailyCapacity = 3 },
            new DiagnosticService { Name = "Urinalysis", DailyCapacity = 3 },
            new DiagnosticService { Name = "Chest X-Ray", DailyCapacity = 2 },
            new DiagnosticService { Name = "ECG", DailyCapacity = 2 });

        await context.SaveChangesAsync();
    }
}
