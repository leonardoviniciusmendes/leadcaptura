using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;

namespace LeadEngine.Infrastructure.GoogleAds;

public sealed class GoogleAdsErrorTranslator : IGoogleAdsErrorTranslator
{
    public GoogleAdsPublicationErrorDto Translate(Exception exception, string? requestId = null)
    {
        var message = exception.Message.ToLowerInvariant();
        var code = message.Contains("auth") || message.Contains("token") ? "authentication"
            : message.Contains("quota") ? "quota"
            : message.Contains("permission") || message.Contains("access") ? "permission"
            : message.Contains("url") ? "invalid_url"
            : message.Contains("policy") ? "policy"
            : message.Contains("keyword") ? "invalid_keyword"
            : message.Contains("budget") ? "invalid_budget"
            : message.Contains("timeout") ? "timeout"
            : "google_ads_error";
        return Translate(code, Friendly(code), null, null, null, null, requestId);
    }

    public GoogleAdsPublicationErrorDto Translate(string code, string message, string? operation, int? index, string? field, string? rejectedValue, string? requestId)
    {
        return new GoogleAdsPublicationErrorDto(code, message, operation, index, field, rejectedValue, requestId, IsRecoverable(code), Suggested(code));
    }

    private static string Friendly(string code) => code switch
    {
        "authentication" => "Falha de autenticacao no Google Ads.",
        "permission" => "A conta conectada nao possui permissao para este customer.",
        "invalid_budget" => "Orcamento invalido para Google Ads.",
        "invalid_url" => "URL final rejeitada pelo Google Ads.",
        "policy" => "Texto possivelmente bloqueado por politica editorial.",
        "invalid_keyword" => "Keyword rejeitada pelo Google Ads.",
        "quota" => "Limite de quota do Google Ads atingido.",
        "timeout" => "Tempo limite ao chamar Google Ads.",
        _ => "Falha retornada pelo Google Ads."
    };

    private static bool IsRecoverable(string code) => code is "quota" or "timeout" or "authentication";
    private static string Suggested(string code) => code switch
    {
        "authentication" => "Reconecte a conta Google Ads.",
        "permission" => "Verifique permissao do customer e developer token.",
        "invalid_budget" => "Ajuste o orcamento no preview.",
        "invalid_url" => "Valide a URL final publicada.",
        "policy" => "Revise headlines e descriptions.",
        "invalid_keyword" => "Revise as keywords do preview.",
        "quota" => "Tente novamente mais tarde.",
        "timeout" => "Tente novamente.",
        _ => "Consulte o requestId e revise a configuracao."
    };
}
