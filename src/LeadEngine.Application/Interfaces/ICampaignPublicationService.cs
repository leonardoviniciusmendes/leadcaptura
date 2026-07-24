using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Interfaces;

public interface ICampaignPublicationService
{
    Task<CampanhaPublicacaoResponse> PublicarAsync(Guid id, CancellationToken cancellationToken);
    Task<CampanhaPublicacaoResponse> DespublicarAsync(Guid id, CancellationToken cancellationToken);
    Task<CampanhaPublicacaoResponse> ObterPublicacaoAsync(Guid id, CancellationToken cancellationToken);
}
