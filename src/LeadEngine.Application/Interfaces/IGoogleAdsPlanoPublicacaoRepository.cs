using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Interfaces;

public interface IGoogleAdsPlanoPublicacaoRepository
{
    Task<GoogleAdsPlanoPublicacao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task<GoogleAdsPlanoPublicacao?> ObterPorCampanhaIdAsync(Guid campanhaId, CancellationToken cancellationToken);
    Task AdicionarAsync(GoogleAdsPlanoPublicacao plano, CancellationToken cancellationToken);
    Task RemoverAsync(GoogleAdsPlanoPublicacao plano, CancellationToken cancellationToken);
    Task SalvarAsync(CancellationToken cancellationToken);
}
