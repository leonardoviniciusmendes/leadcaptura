using LeadEngine.Application.Interfaces;

namespace LeadEngine.Infrastructure.GoogleAds;

public sealed class GoogleAdsGeoTargetResolver : IGoogleAdsGeoTargetResolver
{
    public Task<string> ResolveAsync(string countryCode, CancellationToken cancellationToken)
    {
        return countryCode.Equals("BR", StringComparison.OrdinalIgnoreCase)
            ? Task.FromResult("geoTargetConstants/2076")
            : throw new ArgumentException("Localizacao Google Ads nao suportada nesta etapa.");
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
