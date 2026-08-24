namespace LeadEngine.Application.DTOs;

public sealed record GoogleAdsStatusResponse(
    bool Conectado,
    string Status,
    Guid? ContaPadraoId,
    string? CustomerId,
    string? Nome);

public sealed record GoogleAdsAuthUrlResponse(string Url, string State);

public sealed record GoogleAdsOAuthCallbackRequest(
    string Code,
    string? State,
    string? RedirectUri);

public sealed record GoogleAdsOAuthCallbackResponse(
    bool Sucesso,
    bool Conectado,
    int ContasEncontradas,
    string Mensagem,
    IReadOnlyList<GoogleAdsContaResponse> Contas);

public sealed record GoogleAdsContaResponse(
    Guid Id,
    string CustomerId,
    string CustomerIdMascarado,
    string Nome,
    string? Email,
    bool Ativa,
    bool Padrao,
    string TipoConta,
    bool Gerente,
    DateTime DataConexao,
    DateTime? AccessTokenExpiraEm);

public sealed record GoogleAdsTestarRequest(Guid? ContaId = null);

public sealed record GoogleAdsTestarResponse(
    bool Sucesso,
    string Status,
    string? CustomerId = null,
    long? DuracaoMs = null,
    string? Ambiente = null,
    string? CustomerIdMascarado = null,
    bool TokenRenovado = false,
    bool ContaAcessivel = false,
    bool ConsultaExecutada = false,
    IReadOnlyList<string>? Pendencias = null);

public sealed record GoogleAdsAmbienteResponse(
    string Modo,
    string? CustomerIdMascarado,
    bool ContaCompativel,
    bool PublicacaoPermitida,
    IReadOnlyList<string> Pendencias);

public sealed record GoogleAdsTokenResult(
    string AccessToken,
    string? RefreshToken,
    int ExpiresIn,
    string? Scope,
    string? TokenType);

public sealed record GoogleAdsUserInfo(string? Email, string? Name);

public sealed record GoogleAdsAccessibleAccount(string CustomerId, string Nome);

public sealed record GoogleAdsDiagnosticAccountResponse(
    Guid Id,
    string CustomerId,
    string CustomerIdMascarado,
    string Nome,
    bool Ativa,
    bool Padrao);

public sealed record GoogleAdsDiagnosticCampaignDto(
    string ResourceName,
    string Id,
    string? Name,
    string? Status);

public sealed record GoogleAdsDiagnosticAdGroupDto(
    string ResourceName,
    string Id,
    string? Name,
    string? Status,
    string? CampaignResourceName);

public sealed record GoogleAdsDiagnosticKeywordDto(
    string ResourceName,
    string Id,
    string? Text,
    string? MatchType,
    string? Status,
    string? AdGroupResourceName);

public sealed record GoogleAdsDiagnosticResponsiveSearchAdDto(
    string ResourceName,
    string Id,
    string? Status,
    string? AdGroupResourceName);

public sealed record CreateGoogleAdsDiagnosticCampaignRequest(
    string Name,
    long DailyBudgetMicros);

public sealed record CreateGoogleAdsDiagnosticCampaignResponse(
    string? RequestId,
    IReadOnlyList<GoogleAdsPublishedResourceDto> Resources);

public sealed record CreateGoogleAdsDiagnosticAdGroupRequest(
    string CampaignResourceName,
    string Name);

public sealed record CreateGoogleAdsDiagnosticAdGroupResponse(
    string? RequestId,
    IReadOnlyList<GoogleAdsPublishedResourceDto> Resources);

public sealed record CreateGoogleAdsDiagnosticKeywordItem(
    string Text,
    string MatchType);

public sealed record CreateGoogleAdsDiagnosticKeywordsRequest(
    string AdGroupResourceName,
    IReadOnlyList<CreateGoogleAdsDiagnosticKeywordItem> Keywords);

public sealed record CreateGoogleAdsDiagnosticKeywordsResponse(
    string? RequestId,
    IReadOnlyList<GoogleAdsPublishedResourceDto> Resources);

public sealed record CreateGoogleAdsDiagnosticResponsiveSearchAdRequest(
    string AdGroupResourceName,
    string FinalUrl,
    IReadOnlyList<string> Headlines,
    IReadOnlyList<string> Descriptions);

public sealed record CreateGoogleAdsDiagnosticResponsiveSearchAdResponse(
    string? RequestId,
    IReadOnlyList<GoogleAdsPublishedResourceDto> Resources);
