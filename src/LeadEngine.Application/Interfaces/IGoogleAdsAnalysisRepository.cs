using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Interfaces;

public interface IGoogleAdsAnalysisRepository
{
    Task AdicionarAsync(GoogleAdsAnaliseIa analise, CancellationToken cancellationToken);
    Task<GoogleAdsAnaliseIa?> ObterAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<GoogleAdsAnaliseIa>> ListarPorPublicacaoAsync(Guid publicacaoId, CancellationToken cancellationToken);
    Task SalvarAsync(CancellationToken cancellationToken);
}
