using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeadEngine.Infrastructure.Persistence;

public sealed class MetaAdsVideoRepository(LeadEngineDbContext context) : IMetaAdsVideoRepository
{
    public Task<MetaAdsVideo?> ObterPorConteudoAsync(string adAccountId, string contentHash, CancellationToken cancellationToken)
    {
        return context.MetaAdsVideos
            .FirstOrDefaultAsync(x => x.AdAccountId == adAccountId && x.ContentHash == contentHash, cancellationToken);
    }

    public Task AdicionarAsync(MetaAdsVideo video, CancellationToken cancellationToken)
    {
        return context.MetaAdsVideos.AddAsync(video, cancellationToken).AsTask();
    }

    public Task SalvarAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
