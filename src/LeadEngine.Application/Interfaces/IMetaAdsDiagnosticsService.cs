using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Interfaces;

public interface IMetaAdsDiagnosticsService
{
    Task<MetaAdAccountDto> GetAdAccountAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<MetaCampaignDto>> GetCampaignsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<MetaAdSetDto>> GetAdSetsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<MetaAdDto>> GetAdsAsync(CancellationToken cancellationToken);
    Task<CreateMetaCampaignResponse> CreateCampaignAsync(CreateMetaCampaignRequest request, CancellationToken cancellationToken);
    Task<DeleteMetaCampaignResponse> DeleteCampaignAsync(string campaignId, CancellationToken cancellationToken);
}
