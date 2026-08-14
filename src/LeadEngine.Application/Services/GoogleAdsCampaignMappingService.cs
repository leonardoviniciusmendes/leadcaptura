using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Services;

public sealed class GoogleAdsCampaignMappingService(IConfigurationResolver resolver, CampaignPublicUrlBuilder? publicUrlBuilder = null) : IGoogleAdsCampaignMappingService
{
    public async Task<GoogleAdsPreviewPayload> MapearAsync(Campanha campanha, CancellationToken cancellationToken)
    {
        var config = await Config(cancellationToken);
        var budget = campanha.OrcamentoDiario > 0 ? campanha.OrcamentoDiario : config.DefaultDailyBudget;
        var keywords = Deduplicate(Deserialize<string>(campanha.PalavrasChaveJson))
            .Select(x => new GoogleAdsKeywordPlan(x, MatchTypeFor(x, config), "PAUSED", "Campanha"))
            .ToArray();
        var negatives = Deduplicate(Deserialize<string>(campanha.PalavrasChaveNegativasJson))
            .Select(x => new GoogleAdsNegativeKeywordPlan(x, "PHRASE", "Campanha"))
            .ToArray();
        var (path1, path2) = Paths(campanha.Slug);
        var headlines = Deserialize<string>(campanha.TitulosAnunciosJson).Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
        var descriptions = Deserialize<string>(campanha.DescricoesAnunciosJson).Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
        var urlBuilder = publicUrlBuilder ?? new CampaignPublicUrlBuilder(resolver);
        var url = (await urlBuilder.BuildAsync(campanha.Slug, campanha.UrlPublica, cancellationToken)).Url ?? campanha.UrlPublica ?? string.Empty;
        var cpcMicros = config.DefaultCpcBid is > 0 ? GoogleAdsMoney.ToMicros(config.DefaultCpcBid.Value) : (long?)null;

        return new GoogleAdsPreviewPayload(
            new GoogleAdsCampaignPlan(
                campanha.Nome,
                "SEARCH",
                config.DefaultCampaignStatus,
                "SEARCH",
                true,
                false,
                false,
                campanha.Objetivo ?? "Leads",
                config.DefaultCurrencyCode,
                config.DefaultLanguageCode,
                config.DefaultCountryCode,
                url),
            new GoogleAdsBudgetPlan($"{campanha.Nome} - diario", budget, GoogleAdsMoney.ToMicros(budget), "STANDARD", false),
            [
                new GoogleAdsAdGroupPlan(
                    campanha.Nome.Length > 80 ? campanha.Nome[..80] : campanha.Nome,
                    "PAUSED",
                    config.DefaultCpcBid,
                    cpcMicros,
                    keywords,
                    negatives,
                    new GoogleAdsResponsiveSearchAdPlan(headlines, descriptions, [url], path1, path2, "PAUSED"))
            ]);
    }

    public string CalcularHash(Campanha campanha)
    {
        var source = JsonSerializer.Serialize(new
        {
            campanha.TitulosAnunciosJson,
            campanha.DescricoesAnunciosJson,
            campanha.PalavrasChaveJson,
            campanha.PalavrasChaveNegativasJson,
            campanha.Slug,
            campanha.UrlPublica,
            campanha.OrcamentoDiario,
            campanha.TipoPublico,
            campanha.Regiao,
            campanha.Cidade,
            campanha.Estado,
            campanha.BeneficiosJson
        });
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(source));
        return Convert.ToHexString(bytes);
    }

    private async Task<GoogleAdsConfigurationSnapshot> Config(CancellationToken cancellationToken)
    {
        var publicBaseUrl = (await new CampaignPublicUrlBuilder(resolver).BuildAsync("diagnostico", null, cancellationToken)).PublicBaseUrl;
        return new GoogleAdsConfigurationSnapshot(
            true,
            decimal.TryParse(await Value("DefaultDailyBudget", cancellationToken), NumberStyles.Number, CultureInfo.InvariantCulture, out var budget) ? budget : 10m,
            await Value("DefaultCountryCode", cancellationToken) ?? "BR",
            await Value("DefaultLanguageCode", cancellationToken) ?? "pt",
            await Value("DefaultCurrencyCode", cancellationToken) ?? "BRL",
            await Value("DefaultKeywordMatchType", cancellationToken) ?? "Phrase",
            await Value("DefaultCampaignStatus", cancellationToken) ?? "PAUSED",
            bool.TryParse(await Value("EnableBroadMatch", cancellationToken), out var broad) && broad,
            decimal.TryParse(await Value("DefaultCpcBid", cancellationToken), NumberStyles.Number, CultureInfo.InvariantCulture, out var cpc) ? cpc : null,
            publicBaseUrl);
    }

    private async Task<string?> Value(string key, CancellationToken cancellationToken)
    {
        return (await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, key, cancellationToken)).Value;
    }

    private static string MatchTypeFor(string keyword, GoogleAdsConfigurationSnapshot config)
    {
        if (config.EnableBroadMatch && string.Equals(config.DefaultKeywordMatchType, "Broad", StringComparison.OrdinalIgnoreCase))
        {
            return "BROAD";
        }

        var normalized = RemoveAccents(keyword).ToLowerInvariant();
        return normalized.Contains("cotacao", StringComparison.Ordinal)
            || normalized.Contains("contratar", StringComparison.Ordinal)
            || normalized.Contains("preco", StringComparison.Ordinal)
            ? "EXACT"
            : "PHRASE";
    }

    private static IReadOnlyList<string> Deduplicate(IEnumerable<string> values)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();
        foreach (var value in values.Select(x => x.Trim()).Where(x => x.Length > 0 && x.Length <= 80 && IsValidKeyword(x)))
        {
            var key = RemoveAccents(value).ToLowerInvariant();
            if (seen.Add(key))
            {
                result.Add(value);
            }
        }
        return result;
    }

    private static bool IsValidKeyword(string value)
    {
        return value.All(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || c is '-' or '+' or '&');
    }

    public static (string Path1, string Path2) Paths(string slug)
    {
        var parts = CampanhaText.Slugify(slug).Split('-', StringSplitOptions.RemoveEmptyEntries);
        var path1 = BuildPath(parts.Take(2));
        var path2 = BuildPath(parts.Skip(2).Take(1));
        return (path1, path2);
    }

    private static string BuildPath(IEnumerable<string> parts)
    {
        var path = string.Join('-', parts).Trim('-');
        return path.Length <= 15 ? path : path[..15].Trim('-');
    }

    private static IReadOnlyList<T> Deserialize<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }
        return JsonSerializer.Deserialize<IReadOnlyList<T>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }

    private static string RemoveAccents(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
