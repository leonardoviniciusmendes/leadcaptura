using LeadEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeadEngine.Application.Tests;

public sealed class SegmentCompatibilityTests
{
    [Fact]
    public void SegmentSlug_TemIndiceUnico()
    {
        using var context = Context();
        var entity = context.Model.FindEntityType("LeadEngine.Domain.Entities.Segment");
        var slug = entity!.FindProperty("Slug");

        var index = Assert.Single(entity.GetIndexes(), x => x.Properties.Contains(slug!));
        Assert.True(index.IsUnique);
    }

    [Fact]
    public void Migration_AddSegmentsCompatibility_FazBackfillDeCampanhasExistentes()
    {
        var migration = File.ReadAllText(MigrationPath());

        Assert.Contains("UPDATE `Campanhas` SET `SegmentId` = '3f1ce0a4-7ec5-4c8f-b6d9-df4f3e7f0c35' WHERE `SegmentId` IS NULL;", migration);
    }

    private static LeadEngineDbContext Context()
    {
        var options = new DbContextOptionsBuilder<LeadEngineDbContext>()
            .UseMySql("Server=localhost;Database=leadengine_test;User=test;Password=test;", new MySqlServerVersion(new Version(8, 0, 36)))
            .Options;

        return new LeadEngineDbContext(options);
    }

    private static string MigrationPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "LeadEngine.sln")))
        {
            current = current.Parent;
        }

        if (current is null)
        {
            throw new InvalidOperationException("Nao foi possivel localizar a raiz da solution.");
        }

        return Directory.GetFiles(current.FullName, "*AddSegmentsCompatibility.cs", SearchOption.AllDirectories)
            .Single(x => !x.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase));
    }
}
