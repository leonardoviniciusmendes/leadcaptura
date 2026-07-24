using LeadEngine.Application.DTOs;
using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Interfaces;

public interface IGoogleAdsMetricsRepository
{
    Task<GoogleAdsMetricaDiaria?> ObterAsync(Guid publicacaoId, string campaignExternalId, DateOnly data, CancellationToken cancellationToken);
    Task AdicionarAsync(GoogleAdsMetricaDiaria metrica, CancellationToken cancellationToken);
    Task<IReadOnlyList<GoogleAdsMetricaDiaria>> ListarPorPublicacaoAsync(Guid publicacaoId, DateOnly dataInicial, DateOnly dataFinal, CancellationToken cancellationToken);
    Task<IReadOnlyList<GoogleAdsMetricaDiaria>> ListarAsync(DateOnly dataInicial, DateOnly dataFinal, Guid? campanhaId, Guid? contaId, CancellationToken cancellationToken);
    Task SalvarAsync(CancellationToken cancellationToken);
}
