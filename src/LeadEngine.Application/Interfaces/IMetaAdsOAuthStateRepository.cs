using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Interfaces;

public interface IMetaAdsOAuthStateRepository
{
    Task AdicionarAsync(MetaAdsOAuthState state, CancellationToken cancellationToken);
    Task<MetaAdsOAuthState?> ObterPorHashAsync(string stateHash, CancellationToken cancellationToken);
    Task SalvarAsync(CancellationToken cancellationToken);
}
