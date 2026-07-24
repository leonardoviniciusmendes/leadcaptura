using LeadEngine.Application.DTOs;
using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Interfaces;

public interface IGoogleAdsOperationBuilder
{
    Task<GoogleAdsOperationPlan> BuildAsync(GoogleAdsPlanoPublicacao preview, string customerId, CancellationToken cancellationToken);
}
