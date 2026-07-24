using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Interfaces;

public interface IGoogleAdsSynchronizationService
{
    Task<GoogleAdsSincronizacaoResponse> SincronizarPublicacaoAsync(Guid publicacaoId, CancellationToken cancellationToken);
    Task<GoogleAdsStatusRemotoResponse> ObterStatusRemotoAsync(Guid publicacaoId, CancellationToken cancellationToken);
    Task<IReadOnlyList<GoogleAdsSincronizacaoResponse>> SincronizarTodasAsync(CancellationToken cancellationToken);
    Task<GoogleAdsPublicationResponse> PausarAsync(Guid publicacaoId, CancellationToken cancellationToken);
    Task<GoogleAdsPublicationResponse> AtivarAsync(Guid publicacaoId, GoogleAdsStatusActionRequest request, CancellationToken cancellationToken);
}
