using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Interfaces;

public interface IGoogleAdsCopyAdjustmentService
{
    Task<IReadOnlyList<GoogleAdsCopySuggestionItem>> SugerirAsync(GoogleAdsPreviewPayload payload, IReadOnlyList<string> campos, CancellationToken cancellationToken);
}
