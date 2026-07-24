namespace LeadEngine.Application.Interfaces;

public interface IGoogleAdsGeoTargetResolver
{
    Task<string> ResolveAsync(string countryCode, CancellationToken cancellationToken);
}
