using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Interfaces;

public interface ICampaignReviewService
{
    Task<CampanhaResponse?> ObterRevisaoAsync(Guid id, CancellationToken cancellationToken);
    Task<CampanhaResponse> RevisarCampanhaAsync(Guid id, RevisarCampanhaRequest request, CancellationToken cancellationToken);
    Task<CampanhaResponse> RegenerarSecaoAsync(Guid id, RegenerarCampanhaSecaoRequest request, CancellationToken cancellationToken);
    Task<AprovarCampanhaResponse> AprovarCampanhaAsync(Guid id, AprovarCampanhaRequest request, CancellationToken cancellationToken);
    Task<CreativeQualityGateResponse> ObterCreativeQualityGateAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<CampanhaRevisaoHistoricoResponse>> ListarHistoricoAsync(Guid id, CancellationToken cancellationToken);
}
