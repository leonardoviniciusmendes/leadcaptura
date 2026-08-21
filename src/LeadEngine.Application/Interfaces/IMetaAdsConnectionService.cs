using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Interfaces;

public interface IMetaAdsConnectionService
{
    Task<MetaAdsStatusResponse> ObterStatusAsync(CancellationToken cancellationToken);
    Task<MetaAdsAuthUrlResponse> GerarAuthUrlAsync(CancellationToken cancellationToken);
    Task<MetaAdsAuthUrlResponse> GerarAuthUrlAsync(bool incluirPermissaoPublicacao, CancellationToken cancellationToken);
    Task<MetaAdsOAuthCallbackResponse> ConcluirOAuthAsync(MetaAdsOAuthCallbackRequest request, CancellationToken cancellationToken);
    Task<MetaAdsStatusResponse> DesconectarAsync(CancellationToken cancellationToken);
}
