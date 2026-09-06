using System.Text.Json;
using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Services;

public static class CampaignGenerationContextFactory
{
    public static CampaignGenerationContext FromRequest(GerarCampanhaRequest request, Segment? segment, string? campaignConfigJson)
    {
        var legacy = new CampaignGenerationLegacyContext(
            request.TipoPublico,
            request.Cidade,
            request.Estado,
            request.Regiao,
            request.Operadora,
            request.OperadoraOutra,
            request.OrcamentoDiario,
            request.Objetivo);

        return new CampaignGenerationContext(
            segment?.Name,
            segment?.Slug,
            segment?.TemplateKey,
            ValidateJson(segment?.DefaultConfigJson, $"DefaultConfigJson do segmento '{segment?.Slug ?? "sem-segmento"}'"),
            ValidateJson(campaignConfigJson, "CampaignConfigJson da campanha"),
            CampanhaText.Limitar(request.BusinessDescription, 500),
            CampanhaText.Limitar(request.ProductOrService, 180),
            CampanhaText.Limitar(request.TargetAudience, 300),
            CampanhaText.Limitar(request.CampaignGoal, 300),
            CampanhaText.Limitar(request.Offer, 300),
            NormalizeLocation(request.Location, request.Cidade, request.Estado, request.Regiao),
            CampanhaText.Limitar(request.BrandTone, 120),
            NormalizeRestrictions(request.Restrictions),
            legacy);
    }

    public static CampaignGenerationContext FromCampaign(Campanha campanha)
    {
        return new CampaignGenerationContext(
            campanha.Segment?.Name,
            campanha.Segment?.Slug,
            campanha.Segment?.TemplateKey,
            ValidateJson(campanha.Segment?.DefaultConfigJson, $"DefaultConfigJson do segmento '{campanha.Segment?.Slug ?? "sem-segmento"}'"),
            ValidateJson(campanha.CampaignConfigJson, "CampaignConfigJson da campanha"),
            null,
            null,
            null,
            null,
            null,
            new CampaignLocationDto(campanha.Cidade, campanha.Estado, campanha.Regiao),
            null,
            null,
            new CampaignGenerationLegacyContext(
                campanha.TipoPublico,
                campanha.Cidade,
                campanha.Estado,
                campanha.Regiao,
                campanha.Operadora,
                null,
                campanha.OrcamentoDiario,
                campanha.Objetivo));
    }

    private static string? ValidateJson(string? json, string label)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc.RootElement);
        }
        catch (JsonException ex)
        {
            throw new CampaignGenerationException($"{label} nao contem JSON valido.", ex);
        }
    }

    private static CampaignLocationDto? NormalizeLocation(CampaignLocationDto? location, string? fallbackCity, string? fallbackState, string? fallbackRegion)
    {
        var normalized = new CampaignLocationDto(
            CampanhaText.Limitar(location?.City ?? fallbackCity, 120),
            CampanhaText.Limitar(location?.State ?? fallbackState, 2)?.ToUpperInvariant(),
            CampanhaText.Limitar(location?.Region ?? fallbackRegion, 120));

        return string.IsNullOrWhiteSpace(normalized.City)
            && string.IsNullOrWhiteSpace(normalized.State)
            && string.IsNullOrWhiteSpace(normalized.Region)
            ? null
            : normalized;
    }

    private static IReadOnlyCollection<string>? NormalizeRestrictions(IReadOnlyCollection<string>? restrictions)
    {
        var items = (restrictions ?? [])
            .Select(x => CampanhaText.Limitar(x, 180))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return items.Length == 0 ? null : items;
    }
}
