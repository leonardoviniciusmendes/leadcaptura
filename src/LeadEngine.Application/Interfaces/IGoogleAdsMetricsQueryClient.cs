using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Interfaces;

public interface IGoogleAdsMetricsQueryClient
{
    Task<(string? RequestId, IReadOnlyList<GoogleAdsMetricsRow> Rows)> QueryMetricsAsync(string customerId, string accessToken, string developerToken, string campaignResourceName, DateOnly dataInicial, DateOnly dataFinal, CancellationToken cancellationToken);
}
