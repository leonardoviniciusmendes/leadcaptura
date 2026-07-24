using LeadEngine.Application.DTOs;
using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Interfaces;

public interface IGoogleAdsCampaignMappingService
{
    Task<GoogleAdsPreviewPayload> MapearAsync(Campanha campanha, CancellationToken cancellationToken);
    string CalcularHash(Campanha campanha);
}
