using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Interfaces;

public interface IGoogleAdsDiagnosticsService
{
    Task<GoogleAdsDiagnosticAccountResponse> GetAccountAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<GoogleAdsDiagnosticCampaignDto>> GetCampaignsAsync(CancellationToken cancellationToken);
    Task<CreateGoogleAdsDiagnosticCampaignResponse> CreateCampaignAsync(CreateGoogleAdsDiagnosticCampaignRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<GoogleAdsDiagnosticAdGroupDto>> GetAdGroupsAsync(CancellationToken cancellationToken);
    Task<CreateGoogleAdsDiagnosticAdGroupResponse> CreateAdGroupAsync(CreateGoogleAdsDiagnosticAdGroupRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<GoogleAdsDiagnosticKeywordDto>> GetKeywordsAsync(CancellationToken cancellationToken);
    Task<CreateGoogleAdsDiagnosticKeywordsResponse> CreateKeywordsAsync(CreateGoogleAdsDiagnosticKeywordsRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<GoogleAdsDiagnosticResponsiveSearchAdDto>> GetResponsiveSearchAdsAsync(CancellationToken cancellationToken);
    Task<CreateGoogleAdsDiagnosticResponsiveSearchAdResponse> CreateResponsiveSearchAdAsync(CreateGoogleAdsDiagnosticResponsiveSearchAdRequest request, CancellationToken cancellationToken);
}
