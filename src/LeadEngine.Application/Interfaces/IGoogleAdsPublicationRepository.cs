using LeadEngine.Application.DTOs;
using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Interfaces;

public interface IGoogleAdsPublicationRepository
{
    Task<GoogleAdsPublicacao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task<GoogleAdsPublicacao?> ObterPorPreviewVersaoHashAsync(Guid previewId, int versao, string hash, CancellationToken cancellationToken);
    Task<IReadOnlyList<GoogleAdsPublicacao>> ListarPorCampanhaAsync(Guid campanhaId, CancellationToken cancellationToken);
    Task<IReadOnlyList<GoogleAdsPublicacao>> ListarAsync(GoogleAdsPublicationQuery query, CancellationToken cancellationToken);
    Task AdicionarAsync(GoogleAdsPublicacao publicacao, CancellationToken cancellationToken);
    Task AdicionarRecursoAsync(GoogleAdsRecursoPublicado recurso, CancellationToken cancellationToken);
    Task SalvarAsync(CancellationToken cancellationToken);
}
