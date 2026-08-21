using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Interfaces;

public interface IMetaAdsImagemRepository
{
    Task<MetaAdsImagem?> ObterPorCampanhaAsync(Guid campanhaId, string adAccountId, CancellationToken cancellationToken);
    Task<MetaAdsImagem?> ObterPorConteudoAsync(Guid campanhaId, string adAccountId, string contentHash, CancellationToken cancellationToken);
    Task AdicionarAsync(MetaAdsImagem imagem, CancellationToken cancellationToken);
    Task SalvarAsync(CancellationToken cancellationToken);
}
