using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Interfaces;

public interface IGoogleAdsPublishingService
{
    Task<GoogleAdsRemoteValidationResponse> ValidarRemotamenteAsync(Guid previewId, CancellationToken cancellationToken);
    Task<GoogleAdsDryRunResponse> DryRunAsync(Guid previewId, CancellationToken cancellationToken);
    Task<GoogleAdsPreparePublicationResponse> PrepararAsync(Guid previewId, CancellationToken cancellationToken);
    Task<GoogleAdsPublicationResponse> PublicarAsync(Guid previewId, GoogleAdsPublishRequest request, CancellationToken cancellationToken);
    Task<GoogleAdsReconciliationResponse> ReconciliarAsync(Guid id, CancellationToken cancellationToken);
    Task<GoogleAdsPublicationResponse> ObterAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<GoogleAdsPublicationHistoryResponse>> HistoricoAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<GoogleAdsPublicationResponse>> ListarPorCampanhaAsync(Guid campanhaId, CancellationToken cancellationToken);
    Task<IReadOnlyList<GoogleAdsPublicationResponse>> ListarAsync(GoogleAdsPublicationQuery query, CancellationToken cancellationToken);
}
