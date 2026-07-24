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

public sealed record GoogleAdsContaResponse(
    Guid Id,
    string CustomerId,
    string Nome,
    string? Email,
    bool Ativa,
    bool Padrao,
    DateTime DataConexao,
    DateTime? AccessTokenExpiraEm);

public sealed record GoogleAdsTestarRequest(Guid? ContaId = null);

public sealed record GoogleAdsTestarResponse(bool Sucesso, string Status, string? CustomerId = null, long? DuracaoMs = null);

public sealed record GoogleAdsTokenResult(
    string AccessToken,
    string? RefreshToken,
    int ExpiresIn,
    string? Scope,
    string? TokenType);

public sealed record GoogleAdsUserInfo(string? Email, string? Name);

public sealed record GoogleAdsAccessibleAccount(string CustomerId, string Nome);
