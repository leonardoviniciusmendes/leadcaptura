namespace LeadEngine.Application.Interfaces;

public interface IGoogleAdsLanguageResolver
{
    Task<string> ResolveAsync(string languageCode, CancellationToken cancellationToken);
}
