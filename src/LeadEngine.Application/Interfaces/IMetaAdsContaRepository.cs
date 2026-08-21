using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Interfaces;

public interface IMetaAdsContaRepository
{
    Task<MetaAdsConta?> ObterAtivaAsync(CancellationToken cancellationToken);
    Task<MetaAdsConta?> ObterPorMetaUserIdAsync(string metaUserId, CancellationToken cancellationToken);
    Task AdicionarAsync(MetaAdsConta conta, CancellationToken cancellationToken);
    Task SalvarAsync(CancellationToken cancellationToken);
}
