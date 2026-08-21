using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Interfaces;

public interface IMetaAdsAssetService
{
    Task<MetaAdsAssetListResponse<MetaAdsBusinessResponse>> ListarBusinessesAsync(CancellationToken cancellationToken);
    Task<MetaAdsAssetListResponse<MetaAdsAdAccountResponse>> ListarAdAccountsAsync(string businessId, CancellationToken cancellationToken);
    Task<MetaAdsAssetListResponse<MetaAdsPageResponse>> ListarPagesAsync(CancellationToken cancellationToken);
    Task<MetaAdsAssetListResponse<MetaAdsInstagramAccountResponse>> ObterInstagramAsync(string pageId, CancellationToken cancellationToken);
    Task<MetaAdsAssetListResponse<MetaAdsPixelResponse>> ListarPixelsAsync(string adAccountId, CancellationToken cancellationToken);
    Task<MetaAdsAssetSelectionResponse> ObterSelecaoAsync(CancellationToken cancellationToken);
    Task<MetaAdsAssetSelectionResponse> SalvarSelecaoAsync(MetaAdsAssetSelectionRequest request, CancellationToken cancellationToken);
}
