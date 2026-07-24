using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Interfaces;

public interface IGoogleAdsOAuthStateRepository
{
    Task AdicionarAsync(GoogleAdsOAuthState state, CancellationToken cancellationToken);
    Task<GoogleAdsOAuthState?> ObterPorHashAsync(string stateHash, CancellationToken cancellationToken);
    Task SalvarAsync(CancellationToken cancellationToken);
}
