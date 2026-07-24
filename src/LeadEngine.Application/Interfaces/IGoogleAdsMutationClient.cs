using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Interfaces;

public interface IGoogleAdsMutationClient
{
    Task<GoogleAdsMutationResult> MutateAsync(string customerId, string accessToken, string developerToken, GoogleAdsOperationPlan plan, bool validateOnly, CancellationToken cancellationToken);
    Task<IReadOnlyList<GoogleAdsPublishedResourceDto>> CheckResourcesAsync(string customerId, string accessToken, string developerToken, IReadOnlyList<GoogleAdsPublishedResourceDto> resources, CancellationToken cancellationToken);
}
