using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeadEngine.Infrastructure.Persistence;

public sealed class GoogleAdsMetricsRepository(LeadEngineDbContext context) : IGoogleAdsMetricsRepository
{
    public Task<GoogleAdsMetricaDiaria?> ObterAsync(Guid publicacaoId, string campaignExternalId, DateOnly data, CancellationToken cancellationToken)
    {
        return context.GoogleAdsMetricasDiarias.FirstOrDefaultAsync(x => x.GoogleAdsPublicacaoId == publicacaoId && x.CampaignExternalId == campaignExternalId && x.Data == data, cancellationToken);
    }

    public Task AdicionarAsync(GoogleAdsMetricaDiaria metrica, CancellationToken cancellationToken) => context.GoogleAdsMetricasDiarias.AddAsync(metrica, cancellationToken).AsTask();

    public async Task<IReadOnlyList<GoogleAdsMetricaDiaria>> ListarPorPublicacaoAsync(Guid publicacaoId, DateOnly dataInicial, DateOnly dataFinal, CancellationToken cancellationToken)
    {
        return await context.GoogleAdsMetricasDiarias.Where(x => x.GoogleAdsPublicacaoId == publicacaoId && x.Data >= dataInicial && x.Data <= dataFinal).OrderBy(x => x.Data).ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GoogleAdsMetricaDiaria>> ListarAsync(DateOnly dataInicial, DateOnly dataFinal, Guid? campanhaId, Guid? contaId, CancellationToken cancellationToken)
    {
        var q = context.GoogleAdsMetricasDiarias.Include(x => x.Publicacao).ThenInclude(x => x!.Campanha).AsQueryable().Where(x => x.Data >= dataInicial && x.Data <= dataFinal);
        if (campanhaId is not null) q = q.Where(x => x.Publicacao != null && x.Publicacao.CampanhaId == campanhaId);
        if (contaId is not null) q = q.Where(x => x.GoogleAdsContaId == contaId);
        return await q.ToArrayAsync(cancellationToken);
    }

    public Task SalvarAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
