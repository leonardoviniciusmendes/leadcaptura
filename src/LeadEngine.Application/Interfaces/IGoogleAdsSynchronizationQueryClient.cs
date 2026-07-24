using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Interfaces;

public interface IGoogleAdsSynchronizationQueryClient
{
    Task<GoogleAdsRemoteStatusSnapshot> GetRemoteStatusAsync(string customerId, string accessToken, string developerToken, IReadOnlyList<GoogleAdsPublishedResourceDto> resources, CancellationToken cancellationToken);
    Task SetCampaignStatusAsync(string customerId, string accessToken, string developerToken, string campaignResourceName, string status, CancellationToken cancellationToken);
}
