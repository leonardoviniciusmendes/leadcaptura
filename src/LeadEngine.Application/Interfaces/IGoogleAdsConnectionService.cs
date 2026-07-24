using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Interfaces;

public interface IGoogleAdsConnectionService
{
    Task<GoogleAdsStatusResponse> ObterStatusAsync(CancellationToken cancellationToken);
    Task<GoogleAdsAmbienteResponse> ObterAmbienteAsync(CancellationToken cancellationToken);
    Task<GoogleAdsAuthUrlResponse> GerarAuthUrlAsync(CancellationToken cancellationToken);
    Task<GoogleAdsOAuthCallbackResponse> ConcluirOAuthAsync(GoogleAdsOAuthCallbackRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<GoogleAdsContaResponse>> ListarContasAsync(CancellationToken cancellationToken);
    Task<GoogleAdsContaResponse> SelecionarContaPadraoAsync(Guid id, CancellationToken cancellationToken);
    Task<GoogleAdsTestarResponse> TestarAsync(GoogleAdsTestarRequest request, CancellationToken cancellationToken);
}
