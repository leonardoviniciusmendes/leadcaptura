using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Interfaces;

public interface IMetaAdsPreviewService
{
    Task<MetaAdsPreviewResponse> GerarAsync(MetaAdsPreviewRequest request, CancellationToken cancellationToken);
}
