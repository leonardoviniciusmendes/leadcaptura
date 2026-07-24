using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Interfaces;

public interface IGoogleAdsMetricsService
{
    Task<GoogleAdsSincronizacaoResponse> SincronizarPublicacaoAsync(Guid publicacaoId, GoogleAdsPeriodoRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<GoogleAdsSincronizacaoResponse>> SincronizarTodasAsync(GoogleAdsPeriodoRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<GoogleAdsMetricaDiariaResponse>> ListarPorPublicacaoAsync(Guid publicacaoId, DateOnly? dataInicial, DateOnly? dataFinal, CancellationToken cancellationToken);
    Task<GoogleAdsDashboardResumoResponse> ResumoAsync(DateOnly? dataInicial, DateOnly? dataFinal, Guid? campanhaId, Guid? contaId, CancellationToken cancellationToken);
    Task<IReadOnlyList<GoogleAdsEvolucaoResponse>> EvolucaoAsync(DateOnly? dataInicial, DateOnly? dataFinal, Guid? campanhaId, Guid? contaId, CancellationToken cancellationToken);
    Task<IReadOnlyList<GoogleAdsDashboardCampanhaResponse>> RankingAsync(DateOnly? dataInicial, DateOnly? dataFinal, Guid? campanhaId, Guid? contaId, CancellationToken cancellationToken);
    Task<IReadOnlyList<GoogleAdsAtribuicaoResponse>> AtribuicaoAsync(DateOnly? dataInicial, DateOnly? dataFinal, Guid? campanhaId, Guid? contaId, CancellationToken cancellationToken);
}
