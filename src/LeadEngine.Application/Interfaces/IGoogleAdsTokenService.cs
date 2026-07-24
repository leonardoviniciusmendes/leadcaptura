using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Interfaces;

public interface IGoogleAdsTokenService
{
    Task<string> ObterAccessTokenValidoAsync(GoogleAdsConta conta, CancellationToken cancellationToken);
}
