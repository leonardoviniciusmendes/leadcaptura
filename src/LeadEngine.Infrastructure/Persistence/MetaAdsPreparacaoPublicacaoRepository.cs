using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeadEngine.Infrastructure.Persistence;

public sealed class MetaAdsPreparacaoPublicacaoRepository(LeadEngineDbContext context) : IMetaAdsPreparacaoPublicacaoRepository
{
    public Task<MetaAdsPreparacaoPublicacao?> ObterPorCampanhaAsync(Guid campanhaId, CancellationToken cancellationToken)
    {
        return context.MetaAdsPreparacoesPublicacao.FirstOrDefaultAsync(x => x.CampanhaId == campanhaId, cancellationToken);
    }

    public Task AdicionarAsync(MetaAdsPreparacaoPublicacao preparacao, CancellationToken cancellationToken)
    {
        return context.MetaAdsPreparacoesPublicacao.AddAsync(preparacao, cancellationToken).AsTask();
    }

    public Task SalvarAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
