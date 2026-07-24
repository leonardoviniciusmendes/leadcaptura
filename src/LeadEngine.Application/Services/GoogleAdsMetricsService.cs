using System.Diagnostics;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Services;

public sealed class GoogleAdsMetricsService(
    IGoogleAdsPublicationRepository publicationRepository,
    IGoogleAdsContaRepository contaRepository,
    IGoogleAdsMetricsRepository metricsRepository,
    IGoogleAdsSynchronizationRepository syncRepository,
    IGoogleAdsMetricsQueryClient queryClient,
    IGoogleAdsTokenService tokenService,
    IConfigurationResolver resolver,
    ILeadAttributionService attributionService,
    ILeadRepository leadRepository) : IGoogleAdsMetricsService
{
    public async Task<GoogleAdsSincronizacaoResponse> SincronizarPublicacaoAsync(Guid publicacaoId, GoogleAdsPeriodoRequest request, CancellationToken cancellationToken)
    {
        var (start, end) = GoogleAdsPeriod.Resolve(request.DataInicial, request.DataFinal);
        var publication = await publicationRepository.ObterPorIdAsync(publicacaoId, cancellationToken) ?? throw new KeyNotFoundException("Publicacao Google Ads nao encontrada.");
        var conta = await contaRepository.ObterPorIdAsync(publication.GoogleAdsContaId, cancellationToken) ?? throw new KeyNotFoundException("Conta Google Ads nao encontrada.");
        var campaign = publication.Recursos.FirstOrDefault(x => x.TipoRecurso == "Campaign")?.ResourceName ?? throw new InvalidOperationException("Publicacao sem Campaign resource name.");
        var sync = NewSync(publication, TipoSincronizacaoGoogleAds.Metricas, start, end);
        await syncRepository.AdicionarAsync(sync, cancellationToken);
        await syncRepository.SalvarAsync(cancellationToken);
        var sw = Stopwatch.StartNew();
        try
        {
            var accessToken = await tokenService.ObterAccessTokenValidoAsync(conta, cancellationToken);
            var developerToken = await RequiredSecretAsync("DeveloperToken", cancellationToken);
            var result = await queryClient.QueryMetricsAsync(publication.CustomerId, accessToken, developerToken, campaign, start, end, cancellationToken);
            sync.RequestId = result.RequestId;
            sync.RegistrosConsultados = result.Rows.Count;
            foreach (var row in result.Rows)
            {
                var existing = await metricsRepository.ObterAsync(publication.Id, row.CampaignExternalId, row.Data, cancellationToken);
                if (existing is null)
                {
                    existing = new GoogleAdsMetricaDiaria { Id = Guid.NewGuid(), GoogleAdsPublicacaoId = publication.Id, GoogleAdsContaId = publication.GoogleAdsContaId, CampaignExternalId = row.CampaignExternalId, Data = row.Data, DataCriacao = DateTime.UtcNow };
                    await metricsRepository.AdicionarAsync(existing, cancellationToken);
                    sync.RegistrosCriados++;
                }
                else
                {
                    existing.DataAtualizacao = DateTime.UtcNow;
                    sync.RegistrosAtualizados++;
                }

                Apply(existing, row);
            }

            await attributionService.AtribuirAsync(publication.Id, cancellationToken);
            sync.Status = StatusSincronizacaoGoogleAds.Concluida;
        }
        catch (Exception ex)
        {
            sync.Status = StatusSincronizacaoGoogleAds.Falhou;
            sync.ErroCodigo = "google_ads_metrics_error";
            sync.ErroMensagemControlada = ex.Message;
        }
        finally
        {
            sw.Stop();
            sync.DuracaoMs = sw.ElapsedMilliseconds;
            sync.DataConclusao = DateTime.UtcNow;
            await metricsRepository.SalvarAsync(cancellationToken);
            await syncRepository.SalvarAsync(cancellationToken);
        }

        return ToResponse(sync);
    }

    public async Task<IReadOnlyList<GoogleAdsSincronizacaoResponse>> SincronizarTodasAsync(GoogleAdsPeriodoRequest request, CancellationToken cancellationToken)
    {
        var publications = await publicationRepository.ListarAsync(new GoogleAdsPublicationQuery(StatusPublicacaoGoogleAds.Publicada, null, null, null, null), cancellationToken);
        var result = new List<GoogleAdsSincronizacaoResponse>();
        foreach (var publication in publications)
        {
            result.Add(await SincronizarPublicacaoAsync(publication.Id, request, cancellationToken));
        }
        return result;
    }

    public async Task<IReadOnlyList<GoogleAdsMetricaDiariaResponse>> ListarPorPublicacaoAsync(Guid publicacaoId, DateOnly? dataInicial, DateOnly? dataFinal, CancellationToken cancellationToken)
    {
        var (start, end) = GoogleAdsPeriod.Resolve(dataInicial, dataFinal);
        return (await metricsRepository.ListarPorPublicacaoAsync(publicacaoId, start, end, cancellationToken)).Select(ToMetric).ToArray();
    }

    public async Task<GoogleAdsDashboardResumoResponse> ResumoAsync(DateOnly? dataInicial, DateOnly? dataFinal, Guid? campanhaId, Guid? contaId, CancellationToken cancellationToken)
    {
        var (start, end) = GoogleAdsPeriod.Resolve(dataInicial, dataFinal);
        var metrics = await metricsRepository.ListarAsync(start, end, campanhaId, contaId, cancellationToken);
        var leads = await CountLeadsAsync(start, end, campanhaId, contaId, cancellationToken);
        var cost = metrics.Sum(x => x.Custo);
        var clicks = metrics.Sum(x => x.Cliques);
        var impressions = metrics.Sum(x => x.Impressoes);
        var conversions = metrics.Sum(x => x.Conversoes);
        var value = metrics.Sum(x => x.ValorConversoes);
        var publications = await publicationRepository.ListarAsync(new GoogleAdsPublicationQuery(null, campanhaId, contaId, null, null), cancellationToken);
        return new GoogleAdsDashboardResumoResponse(
            publications.Count,
            publications.Count(x => x.Status == StatusPublicacaoGoogleAds.Publicada),
            publications.Count(x => x.Status is StatusPublicacaoGoogleAds.Publicada or StatusPublicacaoGoogleAds.Reconciliada),
            impressions,
            clicks,
            GoogleAdsMath.SafePercent(clicks, impressions),
            cost,
            GoogleAdsMath.SafeDivide(cost, clicks, 2),
            conversions,
            value,
            leads,
            GoogleAdsMath.SafeDivide(cost, leads, 2),
            GoogleAdsMath.SafePercent(leads, clicks),
            GoogleAdsMath.SafeDivide(value, cost, 4),
            metrics.OrderByDescending(x => x.DataSincronizacao).FirstOrDefault()?.DataSincronizacao,
            leads > 0 ? "Com atribuicao" : "Sem atribuicao");
    }

    public async Task<IReadOnlyList<GoogleAdsEvolucaoResponse>> EvolucaoAsync(DateOnly? dataInicial, DateOnly? dataFinal, Guid? campanhaId, Guid? contaId, CancellationToken cancellationToken)
    {
        var (start, end) = GoogleAdsPeriod.Resolve(dataInicial, dataFinal);
        var metrics = await metricsRepository.ListarAsync(start, end, campanhaId, contaId, cancellationToken);
        return metrics.GroupBy(x => x.Data).OrderBy(x => x.Key).Select(g => new GoogleAdsEvolucaoResponse(g.Key, g.Sum(x => x.Cliques), g.Sum(x => x.Custo), g.Sum(x => x.Conversoes), 0)).ToArray();
    }

    public async Task<IReadOnlyList<GoogleAdsDashboardCampanhaResponse>> RankingAsync(DateOnly? dataInicial, DateOnly? dataFinal, Guid? campanhaId, Guid? contaId, CancellationToken cancellationToken)
    {
        var (start, end) = GoogleAdsPeriod.Resolve(dataInicial, dataFinal);
        var metrics = await metricsRepository.ListarAsync(start, end, campanhaId, contaId, cancellationToken);
        return metrics.GroupBy(x => x.GoogleAdsPublicacaoId).Select(g =>
        {
            var clicks = g.Sum(x => x.Cliques);
            var impressions = g.Sum(x => x.Impressoes);
            var cost = g.Sum(x => x.Custo);
            var conversions = g.Sum(x => x.Conversoes);
            return new GoogleAdsDashboardCampanhaResponse(g.Key, g.First().Publicacao?.Campanha?.Nome ?? g.First().CampaignExternalId, g.First().Publicacao?.Status.ToString() ?? "-", impressions, clicks, GoogleAdsMath.SafePercent(clicks, impressions), cost, conversions, 0, 0, g.Max(x => x.DataSincronizacao));
        }).ToArray();
    }

    public async Task<IReadOnlyList<GoogleAdsAtribuicaoResponse>> AtribuicaoAsync(DateOnly? dataInicial, DateOnly? dataFinal, Guid? campanhaId, Guid? contaId, CancellationToken cancellationToken)
    {
        var query = new LeadQuery(campanhaId, dataInicial?.ToDateTime(TimeOnly.MinValue), dataFinal?.ToDateTime(TimeOnly.MaxValue), null, null, null, null, null, null, null, 1, 1000);
        var leads = (await leadRepository.ListarAsync(query, cancellationToken)).Itens;
        return leads.GroupBy(x => x.TipoAtribuicao).Select(x => new GoogleAdsAtribuicaoResponse(x.Key, x.Count())).ToArray();
    }

    private async Task<int> CountLeadsAsync(DateOnly start, DateOnly end, Guid? campanhaId, Guid? contaId, CancellationToken cancellationToken)
    {
        var query = new LeadQuery(campanhaId, start.ToDateTime(TimeOnly.MinValue), end.ToDateTime(TimeOnly.MaxValue), null, null, null, null, null, null, null, 1, 1000);
        var result = await leadRepository.ListarAsync(query, cancellationToken);
        return result.Itens.Count(x => x.GoogleAdsPublicacaoId is not null || x.TipoAtribuicao != TipoAtribuicaoLead.NaoAtribuida);
    }

    private async Task<string> RequiredSecretAsync(string key, CancellationToken cancellationToken)
    {
        var value = (await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, key, cancellationToken)).Value;
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"{key} nao configurado.");
        return value;
    }

    private static GoogleAdsSincronizacao NewSync(GoogleAdsPublicacao p, TipoSincronizacaoGoogleAds type, DateOnly? start, DateOnly? end) => new() { Id = Guid.NewGuid(), GoogleAdsPublicacaoId = p.Id, GoogleAdsContaId = p.GoogleAdsContaId, Tipo = type, Status = StatusSincronizacaoGoogleAds.Executando, DataInicio = DateTime.UtcNow, DataCriacao = DateTime.UtcNow, PeriodoInicial = start, PeriodoFinal = end };
    private static void Apply(GoogleAdsMetricaDiaria target, GoogleAdsMetricsRow row) { target.CampaignResourceName = row.CampaignResourceName; target.Impressoes = row.Impressoes; target.Cliques = row.Cliques; target.CustoMicros = row.CustoMicros; target.Custo = GoogleAdsMath.MoneyFromMicros(row.CustoMicros); target.Ctr = row.Ctr; target.CpcMedioMicros = row.CpcMedioMicros; target.CpcMedio = GoogleAdsMath.MoneyFromMicros(row.CpcMedioMicros); target.Conversoes = row.Conversoes; target.ValorConversoes = row.ValorConversoes; target.TaxaConversao = GoogleAdsMath.SafePercent(row.Conversoes, row.Cliques); target.ParcelaImpressoesPesquisa = row.ParcelaImpressoesPesquisa; target.TaxaTopoPagina = row.TaxaTopoPagina; target.TaxaTopoAbsoluto = row.TaxaTopoAbsoluto; target.DataSincronizacao = DateTime.UtcNow; }
    private static GoogleAdsMetricaDiariaResponse ToMetric(GoogleAdsMetricaDiaria x) => new(x.Data, x.Impressoes, x.Cliques, x.Custo, x.Ctr, x.CpcMedio, x.Conversoes, x.ValorConversoes, x.TaxaConversao);
    private static GoogleAdsSincronizacaoResponse ToResponse(GoogleAdsSincronizacao x) => new(x.Id, x.GoogleAdsPublicacaoId, x.Tipo, x.Status, x.RegistrosConsultados, x.RegistrosCriados, x.RegistrosAtualizados, x.RequestId, x.ErroMensagemControlada, x.DuracaoMs);
}
