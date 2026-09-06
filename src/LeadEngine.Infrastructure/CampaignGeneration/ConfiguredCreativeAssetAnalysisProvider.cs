using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeadEngine.Infrastructure.CampaignGeneration;

public sealed class ConfiguredCreativeAssetAnalysisProvider(
    IOptions<CreativeAnalysisOptions> options,
    FakeCreativeAssetAnalysisProvider fake,
    OpenRouterCreativeAssetAnalysisProvider openRouter,
    ILogger<ConfiguredCreativeAssetAnalysisProvider> logger) : ICreativeAssetAnalysisProvider
{
    public async Task<CreativeAssetAnalysisProviderResult> AnalyzeAsync(CreativeAssetAnalysisProviderRequest request, CancellationToken cancellationToken)
    {
        var provider = options.Value.Provider;
        if (string.IsNullOrWhiteSpace(provider) || string.Equals(provider, "Fake", StringComparison.OrdinalIgnoreCase))
        {
            return await fake.AnalyzeAsync(request, cancellationToken);
        }

        if (!string.Equals(provider, "OpenRouter", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"CreativeAnalysis Provider '{provider}' nao suportado.");
        }

        try
        {
            return await openRouter.AnalyzeAsync(request, cancellationToken);
        }
        catch (Exception ex) when (options.Value.FallbackToFake && ex is InvalidOperationException or HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "OpenRouter falhou na analise criativa. Fallback Fake ativado explicitamente.");
            return await fake.AnalyzeAsync(request, cancellationToken);
        }
    }
}
