using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Interfaces;

public interface IGoogleAdsResourceQueryClient
{
    Task<IReadOnlyList<GoogleAdsPublishedResourceCheckDto>> CheckResourcesAsync(
        string customerId,
        string accessToken,
        string developerToken,
        IReadOnlyList<GoogleAdsPublishedResourceDto> resources,
        CancellationToken cancellationToken);
}
