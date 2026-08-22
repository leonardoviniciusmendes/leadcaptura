using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Interfaces;

public interface IMetaAdsDiagnosticsService
{
    Task<MetaAdAccountDto> GetAdAccountAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<MetaCampaignDto>> GetCampaignsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<MetaAdSetDto>> GetAdSetsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<MetaAdDto>> GetAdsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<MetaCreativeDto>> GetCreativesAsync(CancellationToken cancellationToken);
    Task<CreateMetaCampaignResponse> CreateCampaignAsync(CreateMetaCampaignRequest request, CancellationToken cancellationToken);
    Task<DeleteMetaCampaignResponse> DeleteCampaignAsync(string campaignId, CancellationToken cancellationToken);
    Task<CreateMetaAdSetResponse> CreateAdSetAsync(CreateMetaAdSetRequest request, CancellationToken cancellationToken);
    Task<DeleteMetaAdSetResponse> DeleteAdSetAsync(string adSetId, CancellationToken cancellationToken);
    Task<CreateMetaCreativeResponse> CreateCreativeAsync(CreateMetaCreativeRequest request, CancellationToken cancellationToken);
    Task<DeleteMetaCreativeResponse> DeleteCreativeAsync(string creativeId, CancellationToken cancellationToken);
    Task<CreateMetaAdResponse> CreateAdAsync(CreateMetaAdRequest request, CancellationToken cancellationToken);
    Task<DeleteMetaAdResponse> DeleteAdAsync(string adId, CancellationToken cancellationToken);
}
