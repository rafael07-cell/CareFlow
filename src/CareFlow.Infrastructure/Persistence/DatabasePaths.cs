namespace CareFlow.Infrastructure.Persistence;

public static class DatabasePaths
{
    public static string ConnectionString
    {
        get
        {
            var password = Environment.GetEnvironmentVariable("CareFlow_DB_PASSWORD");

            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException(
                    "The CareFlow_DB_PASSWORD environment variable is not set. " +
                    "Set it to your local PostgreSQL password before running the application or any 'dotnet ef' command.");

            return $"Host=localhost;Port=5432;Database=CareFlow;Username=postgres;Password={password}";
        }
    }
}
