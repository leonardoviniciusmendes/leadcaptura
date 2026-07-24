using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeadEngine.Infrastructure.CampaignGeneration;

public sealed class ConfiguredCampaignGenerationService(
    IOptions<CampaignGenerationOptions> options,
    FakeCampaignGenerationService fake,
    OpenRouterCampaignGenerationService openRouter,
    ILogger<ConfiguredCampaignGenerationService> logger) : ICampaignGenerationService
{
    public async Task<CampaignGenerationResult> GenerateAsync(GerarCampanhaRequest briefing, CancellationToken cancellationToken)
    {
        var provider = options.Value.Provider.Trim();

        if (string.Equals(provider, "Fake", StringComparison.OrdinalIgnoreCase))
        {
            return await fake.GenerateAsync(briefing, cancellationToken);
        }

        if (!string.Equals(provider, "OpenRouter", StringComparison.OrdinalIgnoreCase))
        {
            throw new CampaignGenerationException($"Provider de geração de campanha inválido: {provider}.");
        }

        try
        {
            return await openRouter.GenerateAsync(briefing, cancellationToken);
        }
        catch (Exception ex) when (options.Value.FallbackToFake)
        {
            logger.LogWarning(ex, "OpenRouter falhou. Fallback Fake ativado explicitamente.");
            return await fake.GenerateAsync(briefing, cancellationToken);
        }
    }
}
