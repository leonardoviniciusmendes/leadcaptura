using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Interfaces;

public interface IMetaAdsPublicacaoRepository
{
    Task<MetaAdsPublicacao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task<MetaAdsPublicacao?> ObterPorCampanhaAdAccountAsync(Guid campanhaId, string adAccountId, CancellationToken cancellationToken);
    Task AdicionarAsync(MetaAdsPublicacao publicacao, CancellationToken cancellationToken);
    Task SalvarAsync(CancellationToken cancellationToken);
}
