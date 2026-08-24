using System.Globalization;
using System.Text;
using LeadEngine.Application.Interfaces;

namespace LeadEngine.Infrastructure.GoogleAds;

public sealed class GoogleAdsGeoTargetResolver : IGoogleAdsGeoTargetResolver
{
    public Task<string> ResolveAsync(string countryCode, CancellationToken cancellationToken)
    {
        return Normalize(countryCode) switch
        {
            "br" => Task.FromResult("geoTargetConstants/2076"),
            "rj" or "br rj" or "state of rio de janeiro" => Task.FromResult("geoTargetConstants/20102"),
            "rio de janeiro" or "rio de janeiro rj" or "rio de janeiro state of rio de janeiro brazil" => Task.FromResult("geoTargetConstants/1001655"),
            _ => throw new ArgumentException("Localizacao Google Ads nao suportada nesta etapa.")
        };
    }

    private static string Normalize(string value)
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

        return string.Join(' ', builder.ToString().Trim().ToLowerInvariant().Split([' ', ',', '-'], StringSplitOptions.RemoveEmptyEntries));
    }
}

public sealed class GoogleAdsLanguageResolver : IGoogleAdsLanguageResolver
{
    public Task<string> ResolveAsync(string languageCode, CancellationToken cancellationToken)
    {
        return languageCode.Equals("pt", StringComparison.OrdinalIgnoreCase)
            ? Task.FromResult("languageConstants/1014")
            : throw new ArgumentException("Idioma Google Ads nao suportado nesta etapa.");
    }
}
