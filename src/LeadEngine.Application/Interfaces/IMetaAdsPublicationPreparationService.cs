using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Interfaces;

public interface IMetaAdsPublicationPreparationService
{
    Task<MetaAdsLocationSearchResponse> BuscarLocalizacoesAsync(string query, CancellationToken cancellationToken);
    Task<MetaAdsLocationResponse> SalvarTargetingAsync(MetaAdsTargetingSelectionRequest request, CancellationToken cancellationToken);
    Task<MetaAdsUploadImageResponse> EnviarImagemAsync(Guid campanhaId, string nomeArquivo, string contentType, byte[] content, CancellationToken cancellationToken);
}
