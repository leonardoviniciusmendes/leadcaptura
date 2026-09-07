using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Interfaces;

public interface IMetaAdsVideoRepository
{
    Task<MetaAdsVideo?> ObterPorConteudoAsync(string adAccountId, string contentHash, CancellationToken cancellationToken);
    Task AdicionarAsync(MetaAdsVideo video, CancellationToken cancellationToken);
    Task SalvarAsync(CancellationToken cancellationToken);
}
