using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.DTOs;

public sealed record GoogleAdsPreviewResponse(
    Guid Id,
    Guid CampanhaId,
    Guid GoogleAdsContaId,
    string ContaNome,
    string CustomerId,
    string NomeCampanha,
    string? Objetivo,
    StatusPlanoPublicacaoGoogleAds Status,
    string TipoRede,
    decimal OrcamentoDiario,
    long OrcamentoMicros,
    string CodigoMoeda,
    string Idioma,
    string Pais,
    string UrlFinal,
    DateTime DataCriacao,
    DateTime? DataAtualizacao,
    DateTime? DataValidacao,
    int Versao,
    IReadOnlyList<string> Erros,
    IReadOnlyList<string> Avisos,
    bool Desatualizado,
    GoogleAdsPreviewPayload Payload,
    GoogleAdsPreviewCounters Contadores);

public sealed record GoogleAdsPreviewCounters(
    int HeadlinesValidas,
    int DescriptionsValidas,
    int Keywords,
    int Negativas,
    int Erros,
    int Avisos);

public sealed record AtualizarGoogleAdsPreviewRequest(
    string? NomeCampanha,
    decimal? OrcamentoDiario,
    string? NomeGrupo,
    decimal? CpcBid,
    IReadOnlyList<GoogleAdsKeywordEditDto>? Keywords,
    IReadOnlyList<GoogleAdsKeywordEditDto>? Negativas,
    IReadOnlyList<string>? Headlines,
    IReadOnlyList<string>? Descriptions,
    string? Path1,
    string? Path2);

public sealed record GoogleAdsKeywordEditDto(string Texto, string? MatchType);

public sealed record GoogleAdsSugerirAjustesRequest(IReadOnlyList<string>? Campos);

public sealed record GoogleAdsCopySuggestionResponse(
    Guid PreviewId,
    IReadOnlyList<GoogleAdsCopySuggestionItem> Sugestoes);

public sealed record GoogleAdsCopySuggestionItem(
    string Campo,
    int Indice,
    string Original,
    string Sugestao,
    int Limite);

public sealed record AplicarGoogleAdsSugestaoRequest(
    string Campo,
    int Indice,
    string Sugestao);

public sealed record GoogleAdsPreviewPayload(
    GoogleAdsCampaignPlan Campaign,
    GoogleAdsBudgetPlan Budget,
    IReadOnlyList<GoogleAdsAdGroupPlan> AdGroups);

public sealed record GoogleAdsCampaignPlan(
    string Name,
    string AdvertisingChannelType,
    string Status,
    string Network,
    bool IncludeGoogleSearch,
    bool IncludeSearchPartners,
    bool IncludeDisplayNetwork,
    string Objective,
    string CurrencyCode,
    string LanguageCode,
    string CountryCode,
    string FinalUrl);

public sealed record GoogleAdsBudgetPlan(
    string Name,
    decimal Amount,
    long AmountMicros,
    string DeliveryMethod,
    bool Shared);

public sealed record GoogleAdsAdGroupPlan(
    string Name,
    string Status,
    decimal? CpcBid,
    long? CpcBidMicros,
    IReadOnlyList<GoogleAdsKeywordPlan> Keywords,
    IReadOnlyList<GoogleAdsNegativeKeywordPlan> NegativeKeywords,
    GoogleAdsResponsiveSearchAdPlan ResponsiveSearchAd);

public sealed record GoogleAdsKeywordPlan(string Text, string MatchType, string Status, string Origem);

public sealed record GoogleAdsNegativeKeywordPlan(string Text, string MatchType, string Origem);

public sealed record GoogleAdsResponsiveSearchAdPlan(
    IReadOnlyList<string> Headlines,
    IReadOnlyList<string> Descriptions,
    IReadOnlyList<string> FinalUrls,
    string Path1,
    string Path2,
    string Status);
