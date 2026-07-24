using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Interfaces;

public interface IGoogleAdsOptimizationService
{
    Task<GoogleAdsAnaliseResponse> AnalisarAsync(Guid publicacaoId, GoogleAdsPeriodoRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<GoogleAdsAnaliseResponse>> ListarAsync(Guid publicacaoId, CancellationToken cancellationToken);
    Task<GoogleAdsAnaliseResponse> ObterAsync(Guid id, CancellationToken cancellationToken);
    Task<GoogleAdsPreviewResponse> CriarPreviewAsync(Guid analiseId, GoogleAdsCriarPreviewPorAnaliseRequest request, CancellationToken cancellationToken);
}
