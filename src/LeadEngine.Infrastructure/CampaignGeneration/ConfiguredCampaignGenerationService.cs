using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeadEngine.Infrastructure.CampaignGeneration;

public sealed class ConfiguredCampaignGenerationService(
    IOptions<CampaignGenerationOptions> options,
    FakeCampaignGenerationService fake,
    OpenRouterCampaignGenerationService openRouter,
    ILogger<ConfiguredCampaignGenerationService> logger,
    IConfigurationResolver? resolver = null) : ICampaignGenerationService
{
    public async Task<CampaignGenerationResult> GenerateAsync(GerarCampanhaRequest briefing, CancellationToken cancellationToken)
    {
        var provider = (resolver is null ? null : (await resolver.ResolveAsync(CategoriaConfiguracao.CampaignGeneration, "Provider", cancellationToken)).Value)
            ?? options.Value.Provider;
        provider = provider.Trim();

        if (string.Equals(provider, "Fake", StringComparison.OrdinalIgnoreCase))
        {
            return await fake.GenerateAsync(briefing, cancellationToken);
        }

        if (!string.Equals(provider, "OpenRouter", StringComparison.OrdinalIgnoreCase))
        {
            throw new CampaignGenerationException($"Provider de geração de campanha inválido: {provider}.");
        }

        var fallbackToFake = await FallbackToFakeAsync(cancellationToken);
        try
        {
            return await openRouter.GenerateAsync(briefing, cancellationToken);
        }
        catch (Exception ex) when (fallbackToFake)
        {
            logger.LogWarning(ex, "OpenRouter falhou. Fallback Fake ativado explicitamente.");
            return await fake.GenerateAsync(briefing, cancellationToken);
        }
    }

    private async Task<bool> FallbackToFakeAsync(CancellationToken cancellationToken)
    {
        var configured = resolver is null ? null : (await resolver.ResolveAsync(CategoriaConfiguracao.CampaignGeneration, "FallbackToFake", cancellationToken)).Value;
        return bool.TryParse(configured, out var value) ? value : options.Value.FallbackToFake;
    }
}
