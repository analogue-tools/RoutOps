using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TravelOptimizer.Persistence;

/// <summary>
/// Used only by the EF tooling (`dotnet ef migrations`) so the design-time build doesn't need the
/// Api host. The connection string is a placeholder — migrations don't connect to generate code.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=TravelOptimizer;Username=admin;Password=root;Include Error Detail=True")
            .Options;

        return new AppDbContext(options);
    }
}
