using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Interfaces;

public interface IGoogleAdsOAuthClient
{
    Task<GoogleAdsTokenResult> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken);
    Task<GoogleAdsTokenResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    Task<GoogleAdsUserInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken);
    Task<IReadOnlyList<GoogleAdsAccessibleAccount>> ListAccessibleAccountsAsync(string accessToken, CancellationToken cancellationToken);
    Task TestConnectionAsync(string accessToken, string customerId, CancellationToken cancellationToken);
}
