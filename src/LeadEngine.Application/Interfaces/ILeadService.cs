using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Interfaces;

public interface ILeadService
{
    Task<CampanhaPublicaResponse?> ObterCampanhaPublicaAsync(string slug, CancellationToken cancellationToken);
    Task<CapturarLeadPublicoResponse> CapturarLeadPublicoAsync(string slug, CapturarLeadPublicoRequest request, CancellationToken cancellationToken);
}
