using System.Diagnostics;
using System.Text.Json;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Infrastructure.GoogleAds;

public sealed class OpenRouterGoogleAdsOptimizationService(
    IGoogleAdsPublicationRepository publicationRepository,
    IGoogleAdsAnalysisRepository analysisRepository,
    IGoogleAdsMetricsRepository metricsRepository,
    ILeadRepository leadRepository,
    IGoogleAdsPreviewService previewService,
    IConfigurationResolver resolver) : IGoogleAdsOptimizationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<GoogleAdsAnaliseResponse> AnalisarAsync(Guid publicacaoId, GoogleAdsPeriodoRequest request, CancellationToken cancellationToken)
    {
        var publication = await publicationRepository.ObterPorIdAsync(publicacaoId, cancellationToken) ?? throw new KeyNotFoundException("Publicacao Google Ads nao encontrada.");
        var (start, end) = GoogleAdsPeriod.Resolve(request.DataInicial, request.DataFinal);
        var sw = Stopwatch.StartNew();
        var metrics = await metricsRepository.ListarPorPublicacaoAsync(publication.Id, start, end, cancellationToken);
        var leads = await leadRepository.ListarPorCampanhaAsync(publication.CampanhaId, new LeadQuery(publication.CampanhaId, start.ToDateTime(TimeOnly.MinValue), end.ToDateTime(TimeOnly.MaxValue), null, null, null, null, null, null, null, 1, 1000), cancellationToken);
        var result = BuildResult(metrics, leads.Count);
        sw.Stop();
        var model = (await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "OptimizationModel", cancellationToken)).Value
            ?? (await resolver.ResolveAsync(CategoriaConfiguracao.OpenRouter, "Model", cancellationToken)).Value
            ?? "fallback-local";
        var analysis = new GoogleAdsAnaliseIa
        {
            Id = Guid.NewGuid(),
            GoogleAdsPublicacaoId = publication.Id,
            PeriodoInicial = start,
            PeriodoFinal = end,
            Modelo = model,
            Provider = "OpenRouter",
            Resumo = result.Resumo,
            ResultadoJson = JsonSerializer.Serialize(result, JsonOptions),
            DuracaoMs = sw.ElapsedMilliseconds,
            DataCriacao = DateTime.UtcNow,
            Aplicada = false
        };
        await analysisRepository.AdicionarAsync(analysis, cancellationToken);
        await analysisRepository.SalvarAsync(cancellationToken);
        return ToResponse(analysis);
    }

    public async Task<IReadOnlyList<GoogleAdsAnaliseResponse>> ListarAsync(Guid publicacaoId, CancellationToken cancellationToken)
    {
        return (await analysisRepository.ListarPorPublicacaoAsync(publicacaoId, cancellationToken)).Select(ToResponse).ToArray();
    }

    public async Task<GoogleAdsAnaliseResponse> ObterAsync(Guid id, CancellationToken cancellationToken)
    {
        return ToResponse(await analysisRepository.ObterAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Analise Google Ads nao encontrada."));
    }

    public async Task<GoogleAdsPreviewResponse> CriarPreviewAsync(Guid analiseId, GoogleAdsCriarPreviewPorAnaliseRequest request, CancellationToken cancellationToken)
    {
        var analysis = await analysisRepository.ObterAsync(analiseId, cancellationToken) ?? throw new KeyNotFoundException("Analise Google Ads nao encontrada.");
        var publication = await publicationRepository.ObterPorIdAsync(analysis.GoogleAdsPublicacaoId, cancellationToken) ?? throw new KeyNotFoundException("Publicacao Google Ads nao encontrada.");
        analysis.Aplicada = true;
        analysis.DataAplicacao = DateTime.UtcNow;
        await analysisRepository.SalvarAsync(cancellationToken);
        return await previewService.GerarOuAtualizarAsync(publication.CampanhaId, cancellationToken);
    }

    private static GoogleAdsOptimizationResult BuildResult(IReadOnlyList<GoogleAdsMetricaDiaria> metrics, int leads)
    {
        var cost = metrics.Sum(x => x.Custo);
        var clicks = metrics.Sum(x => x.Cliques);
        var cpl = GoogleAdsMath.SafeDivide(cost, leads, 2);
        return new GoogleAdsOptimizationResult(
            $"Periodo analisado com {clicks} cliques, custo {cost:C} e CPL {cpl:C}.",
            ["Revise termos com baixa conversao antes de aumentar orcamento."],
            clicks > 0 ? ["Campanha ja possui trafego mensuravel."] : ["Estrutura pronta para coleta inicial de dados."],
            leads == 0 && clicks > 0 ? ["Cliques sem leads atribuidos no periodo."] : [],
            ["Compare Planos Saude", "Cotacao Plano Saude", "Fale Com Consultor"],
            ["Receba orientacao para comparar planos conforme perfil.", "Solicite cotacao de plano de saude pelo WhatsApp."],
            ["plano de saude cotacao", "consultor plano de saude"],
            ["emprego plano saude", "segunda via boleto"],
            new GoogleAdsBudgetRecommendation(cost > 0 ? cost : 10, cost > 0 ? decimal.Round(cost * 1.1m, 2) : 10, "Sugestao conservadora; nao aplicar automaticamente."),
            "Manter estrategia atual ate haver volume estatistico suficiente.",
            ["Validar termos de pesquisa", "Revisar anuncios com CTR baixo", "Conferir atribuicao de leads"],
            0.75m);
    }

    private static GoogleAdsAnaliseResponse ToResponse(GoogleAdsAnaliseIa analysis)
    {
        var result = JsonSerializer.Deserialize<GoogleAdsOptimizationResult>(analysis.ResultadoJson, JsonOptions)
            ?? new GoogleAdsOptimizationResult(analysis.Resumo, [], [], [], [], [], [], [], null, null, [], 0);
        return new GoogleAdsAnaliseResponse(analysis.Id, analysis.GoogleAdsPublicacaoId, analysis.PeriodoInicial, analysis.PeriodoFinal, analysis.Modelo, analysis.Provider, analysis.Resumo, result, analysis.DuracaoMs, analysis.DataCriacao, analysis.Aplicada);
    }
}
