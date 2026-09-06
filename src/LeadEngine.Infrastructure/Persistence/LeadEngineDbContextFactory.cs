using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LeadEngine.Infrastructure.Persistence;

public sealed class LeadEngineDbContextFactory : IDesignTimeDbContextFactory<LeadEngineDbContext>
{
    public LeadEngineDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LeadEngineDbContext>();
        optionsBuilder.UseMySql(
    "Server=localhost;Port=3306;Database=leadengine;User=root;Password=ligado01;SslMode=None;AllowPublicKeyRetrieval=True;Allow User Variables=true",
            new MySqlServerVersion(new Version(8, 0, 36)));

        return new LeadEngineDbContext(optionsBuilder.Options);
    }
}
