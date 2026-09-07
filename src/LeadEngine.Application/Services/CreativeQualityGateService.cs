using System.Text.Json;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Services;

public sealed class CreativeQualityGateService(ICreativeAssetRepository creativeAssetRepository)
{
    public async Task<CreativeQualityGateResponse> EvaluateAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        var selected = (await creativeAssetRepository.ListarPorCampanhaAsync(campaignId, cancellationToken))
            .FirstOrDefault(x => x.IsSelected);
        if (selected is null)
        {
            return new CreativeQualityGateResponse(
                "NO_CREATIVE",
                null,
                null,
                null,
                null,
                null,
                true,
                false,
                ["Nenhuma imagem principal selecionada."]);
        }

        var analysis = selected.Analyses.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
        if (analysis is null)
        {
            return new CreativeQualityGateResponse(
                "NOT_ANALYZED",
                selected.Id,
                null,
                selected.FileName,
                null,
                null,
                true,
                false,
                ["Esta imagem ainda nao foi analisada."]);
        }

        var scores = AnalysisScores(analysis);
        var score = CreativeAnalysisScoreNormalizer.RankingScore(scores);
        if (scores.SemanticMismatch || score <= 35)
        {
            var reasons = new List<string>();
            if (scores.SemanticMismatch)
            {
                reasons.Add("A imagem principal nao corresponde semanticamente a campanha.");
            }

            if (score <= 35)
            {
                reasons.Add("Score de qualidade abaixo do minimo recomendado.");
            }

            return Response("BLOCKED", selected, analysis, score, scores.SemanticMismatch, false, true, reasons);
        }

        if (score <= 69)
        {
            return Response(
                "WARNING",
                selected,
                analysis,
                score,
                scores.SemanticMismatch,
                true,
                false,
                ["Atencao: criativo com qualidade intermediaria."]);
        }

        return Response(
            "APPROVED",
            selected,
            analysis,
            score,
            scores.SemanticMismatch,
            true,
            false,
            ["Criativo aprovado pela analise de qualidade."]);
    }

    private static CreativeQualityGateResponse Response(
        string status,
        CreativeAsset asset,
        CreativeAssetAnalysis analysis,
        int score,
        bool semanticMismatch,
        bool canApprove,
        bool requiresOverride,
        IReadOnlyList<string> reasons)
    {
        return new CreativeQualityGateResponse(
            status,
            asset.Id,
            analysis.Id,
            asset.FileName,
            score,
            semanticMismatch,
            canApprove,
            requiresOverride,
            reasons);
    }

    private static CreativeAnalysisScores AnalysisScores(CreativeAssetAnalysis analysis)
    {
        try
        {
            using var doc = JsonDocument.Parse(analysis.RawResponseJson);
            return CreativeAnalysisScoreNormalizer.FromJson(doc.RootElement, analysis.VisualQualityScore, analysis.BrandFitScore, analysis.BrandFitScore);
        }
        catch (JsonException)
        {
            return new CreativeAnalysisScores(analysis.VisualQualityScore, analysis.BrandFitScore, analysis.BrandFitScore, analysis.TextDensityScore, analysis.BrandFitScore, false, false, null, 100);
        }
    }
}
