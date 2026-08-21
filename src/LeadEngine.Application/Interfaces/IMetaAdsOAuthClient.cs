using LeadEngine.Application.DTOs;
using LeadEngine.Application.Services;

namespace LeadEngine.Application.Interfaces;

public interface IMetaAdsOAuthClient
{
    string BuildAuthorizationUrl(MetaAdsConfiguration config, string state);
    Task<MetaAdsTokenResult> ExchangeCodeAsync(string code, CancellationToken cancellationToken);
    Task<MetaAdsUserInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken);
}
