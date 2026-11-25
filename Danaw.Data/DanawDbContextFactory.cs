using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Danaw.Data;

// Used only by the EF tools (dotnet ef) at design time to create migrations.
public class DanawDbContextFactory : IDesignTimeDbContextFactory<DanawDbContext>
{
    public DanawDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DanawDbContext>()
            .UseSqlite("Data Source=danaw-design.db")
            .Options;
        return new DanawDbContext(options);
    }
}