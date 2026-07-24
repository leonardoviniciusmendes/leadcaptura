using LeadEngine.Application.DTOs;
using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Interfaces;

public interface IGoogleAdsRemoteValidationService
{
    Task<GoogleAdsMutationResult> ValidarAsync(GoogleAdsPlanoPublicacao preview, GoogleAdsConta conta, CancellationToken cancellationToken);
}
