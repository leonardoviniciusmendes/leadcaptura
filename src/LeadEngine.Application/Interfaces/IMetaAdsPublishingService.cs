using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Interfaces;

public interface IMetaAdsPublishingService
{
    Task<MetaAdsPublicationStatusResponse> ObterPorCampanhaAsync(Guid campanhaId, CancellationToken cancellationToken);
    Task<MetaAdsPublicacaoResponse> PublicarAsync(Guid campanhaId, CancellationToken cancellationToken);
    Task<MetaAdsPublicacaoResponse> RetentarAsync(Guid publicacaoId, CancellationToken cancellationToken);
}
