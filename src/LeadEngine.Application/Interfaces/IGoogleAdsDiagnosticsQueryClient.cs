using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Interfaces;

public interface IGoogleAdsDiagnosticsQueryClient
{
    Task<IReadOnlyList<GoogleAdsDiagnosticCampaignDto>> GetCampaignsAsync(
        string customerId,
        string accessToken,
        string developerToken,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<GoogleAdsDiagnosticAdGroupDto>> GetAdGroupsAsync(
        string customerId,
        string accessToken,
        string developerToken,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<GoogleAdsDiagnosticKeywordDto>> GetKeywordsAsync(
        string customerId,
        string accessToken,
        string developerToken,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<GoogleAdsDiagnosticResponsiveSearchAdDto>> GetResponsiveSearchAdsAsync(
        string customerId,
        string accessToken,
        string developerToken,
        CancellationToken cancellationToken);
}
