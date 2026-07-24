using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Interfaces;

public interface IGoogleAdsPreviewService
{
    Task<GoogleAdsPreviewResponse> GerarOuAtualizarAsync(Guid campanhaId, CancellationToken cancellationToken);
    Task<GoogleAdsPreviewResponse> ObterAsync(Guid id, CancellationToken cancellationToken);
    Task<GoogleAdsPreviewResponse> ObterPorCampanhaAsync(Guid campanhaId, CancellationToken cancellationToken);
    Task<GoogleAdsPreviewResponse> ValidarAsync(Guid id, CancellationToken cancellationToken);
    Task<GoogleAdsPreviewResponse> AtualizarAsync(Guid id, AtualizarGoogleAdsPreviewRequest request, CancellationToken cancellationToken);
    Task<GoogleAdsCopySuggestionResponse> SugerirAjustesAsync(Guid id, GoogleAdsSugerirAjustesRequest request, CancellationToken cancellationToken);
    Task<GoogleAdsPreviewResponse> AplicarSugestaoAsync(Guid id, AplicarGoogleAdsSugestaoRequest request, CancellationToken cancellationToken);
    Task<GoogleAdsPreviewPayload> ObterPayloadAsync(Guid id, CancellationToken cancellationToken);
    Task ExcluirAsync(Guid id, CancellationToken cancellationToken);
}
