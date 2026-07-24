using LeadEngine.Application.DTOs;
using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Interfaces;

public interface IGoogleAdsValidationService
{
    GoogleAdsValidationResult ValidarEntrada(Campanha? campanha, GoogleAdsConta? conta, GoogleAdsConfigurationSnapshot config);
    GoogleAdsValidationResult ValidarPayload(GoogleAdsPreviewPayload payload, GoogleAdsConfigurationSnapshot config);
}

public sealed record GoogleAdsValidationResult(IReadOnlyList<string> Erros, IReadOnlyList<string> Avisos)
{
    public bool Valido => Erros.Count == 0;
}

public sealed record GoogleAdsConfigurationSnapshot(
    bool ConfiguracaoValida,
    decimal DefaultDailyBudget,
    string DefaultCountryCode,
    string DefaultLanguageCode,
    string DefaultCurrencyCode,
    string DefaultKeywordMatchType,
    string DefaultCampaignStatus,
    bool EnableBroadMatch,
    decimal? DefaultCpcBid,
    string? PublicBaseUrl);
