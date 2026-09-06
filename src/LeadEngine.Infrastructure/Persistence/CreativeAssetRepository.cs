using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeadEngine.Infrastructure.Persistence;

public sealed class CreativeAssetRepository(LeadEngineDbContext context) : ICreativeAssetRepository
{
    public Task<CreativeAsset?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return context.CreativeAssets
            .Include(x => x.Analyses)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<CreativeAsset>> ListarPorCampanhaAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        return await context.CreativeAssets
            .Include(x => x.Analyses)
            .Where(x => x.CampaignId == campaignId)
            .OrderByDescending(x => x.CreatedAt)
            .ToArrayAsync(cancellationToken);
    }

    public Task AdicionarAsync(CreativeAsset asset, CancellationToken cancellationToken)
    {
        return context.CreativeAssets.AddAsync(asset, cancellationToken).AsTask();
    }

    public Task AdicionarAnaliseAsync(CreativeAssetAnalysis analysis, CancellationToken cancellationToken)
    {
        return context.CreativeAssetAnalyses.AddAsync(analysis, cancellationToken).AsTask();
    }

    public Task SalvarAsync(CancellationToken cancellationToken)
    {
        return context.SaveChangesAsync(cancellationToken);
    }
}
