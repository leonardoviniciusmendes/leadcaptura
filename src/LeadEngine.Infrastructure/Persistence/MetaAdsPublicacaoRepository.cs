using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeadEngine.Infrastructure.Persistence;

public sealed class MetaAdsPublicacaoRepository(LeadEngineDbContext context) : IMetaAdsPublicacaoRepository
{
    public Task<MetaAdsPublicacao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return context.MetaAdsPublicacoes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<MetaAdsPublicacao?> ObterPorCampanhaAdAccountAsync(Guid campanhaId, string adAccountId, CancellationToken cancellationToken)
    {
        return context.MetaAdsPublicacoes.FirstOrDefaultAsync(x => x.CampanhaId == campanhaId && x.AdAccountId == adAccountId, cancellationToken);
    }

    public Task AdicionarAsync(MetaAdsPublicacao publicacao, CancellationToken cancellationToken)
    {
        return context.MetaAdsPublicacoes.AddAsync(publicacao, cancellationToken).AsTask();
    }

    public Task SalvarAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
