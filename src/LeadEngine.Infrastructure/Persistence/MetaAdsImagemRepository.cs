using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeadEngine.Infrastructure.Persistence;

public sealed class MetaAdsImagemRepository(LeadEngineDbContext context) : IMetaAdsImagemRepository
{
    public Task<MetaAdsImagem?> ObterPorCampanhaAsync(Guid campanhaId, string adAccountId, CancellationToken cancellationToken)
    {
        return context.MetaAdsImagens
            .OrderByDescending(x => x.DataUpload)
            .FirstOrDefaultAsync(x => x.CampanhaId == campanhaId && x.AdAccountId == adAccountId, cancellationToken);
    }

    public Task<MetaAdsImagem?> ObterPorConteudoAsync(Guid campanhaId, string adAccountId, string contentHash, CancellationToken cancellationToken)
    {
        return context.MetaAdsImagens
            .FirstOrDefaultAsync(x => x.CampanhaId == campanhaId && x.AdAccountId == adAccountId && x.ContentHash == contentHash, cancellationToken);
    }

    public Task AdicionarAsync(MetaAdsImagem imagem, CancellationToken cancellationToken)
    {
        return context.MetaAdsImagens.AddAsync(imagem, cancellationToken).AsTask();
    }

    public Task SalvarAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
