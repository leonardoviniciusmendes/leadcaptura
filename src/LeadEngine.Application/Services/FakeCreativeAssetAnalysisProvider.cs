using System.Text.Json;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;

namespace LeadEngine.Application.Services;

public sealed class FakeCreativeAssetAnalysisProvider : ICreativeAssetAnalysisProvider
{
    public Task<CreativeAssetAnalysisProviderResult> AnalyzeAsync(CreativeAssetAnalysisProviderRequest request, CancellationToken cancellationToken)
    {
        var landscape = request.Width >= request.Height;
        var mediaLabel = string.Equals(request.MediaType, "Video", StringComparison.OrdinalIgnoreCase) ? "Video" : "Imagem";
        var quality = Math.Clamp((request.Width * request.Height) >= 1_000_000 ? 90 : 76, 0, 100);
        var brandFit = string.IsNullOrWhiteSpace(request.BrandTone) ? 78 : 86;
        var textDensity = 35;
        var payload = new
        {
            summary = $"{mediaLabel} {request.FileName} adequado para revisao criativa antes da publicacao.",
            detectedText = "",
            visualQualityScore = quality,
            campaignFitScore = 84,
            brandFitScore = brandFit,
            textDensityScore = textDensity,
            messageConsistencyScore = 82,
            semanticMismatch = false,
            placementRecommendations = new Dictionary<string, string>
            {
                ["facebookFeed"] = "recommended",
                ["instagramFeed"] = "recommended",
                ["stories"] = landscape ? "needs_crop" : "recommended",
                ["reels"] = landscape ? "not_recommended" : "needs_crop"
            },
            risks = Array.Empty<string>(),
            suggestedCopy = new
            {
                headline = string.IsNullOrWhiteSpace(request.ProductOrService) ? "Conheca a oferta" : request.ProductOrService,
                primaryText = string.IsNullOrWhiteSpace(request.Offer) ? "Veja as melhores opcoes para seu perfil." : request.Offer,
                description = request.CampaignGoal ?? "Fale com um especialista.",
                cta = "LEARN_MORE"
            }
        };

        return Task.FromResult(new CreativeAssetAnalysisProviderResult(
            "Fake",
            "fake-vision-v1",
            JsonSerializer.Serialize(payload)));
    }
}
