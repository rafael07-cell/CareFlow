using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CareFlow.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CareFlowDbContext>
{
    public CareFlowDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CareFlowDbContext>()
            .UseNpgsql(DatabasePaths.ConnectionString)
            .Options;
        return new CareFlowDbContext(options);
    }
}
