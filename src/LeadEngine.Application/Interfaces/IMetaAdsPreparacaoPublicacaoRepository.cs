using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Interfaces;

public interface IMetaAdsPreparacaoPublicacaoRepository
{
    Task<MetaAdsPreparacaoPublicacao?> ObterPorCampanhaAsync(Guid campanhaId, CancellationToken cancellationToken);
    Task AdicionarAsync(MetaAdsPreparacaoPublicacao preparacao, CancellationToken cancellationToken);
    Task SalvarAsync(CancellationToken cancellationToken);
}
