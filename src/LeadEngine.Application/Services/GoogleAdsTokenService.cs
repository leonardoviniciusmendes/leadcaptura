using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Services;

public sealed class GoogleAdsTokenService(
    IGoogleAdsOAuthClient oauthClient,
    ISecretProtector protector,
    IGoogleAdsContaRepository repository) : IGoogleAdsTokenService
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(2);
    private const string RefreshTokenInvalido = "Refresh token invalido ou revogado. Reconecte a conta Google.";

    public async Task<string> ObterAccessTokenValidoAsync(GoogleAdsConta conta, CancellationToken cancellationToken)
    {
        if (conta.AccessTokenExpiraEm is not null
            && conta.AccessTokenExpiraEm.Value > DateTime.UtcNow.Add(RefreshSkew)
            && !string.IsNullOrWhiteSpace(conta.AccessTokenProtegido))
        {
            return protector.Unprotect(conta.AccessTokenProtegido);
        }

        if (string.IsNullOrWhiteSpace(conta.RefreshTokenProtegido))
        {
            throw new InvalidOperationException("Refresh token do Google Ads nao configurado.");
        }

        var refreshToken = protector.Unprotect(conta.RefreshTokenProtegido);
        GoogleAdsTokenResult token;
        try
        {
            token = await oauthClient.RefreshAsync(refreshToken, cancellationToken);
        }
        catch (InvalidOperationException ex) when (string.Equals(ex.Message, RefreshTokenInvalido, StringComparison.Ordinal))
        {
            conta.AccessTokenProtegido = null;
            conta.RefreshTokenProtegido = null;
            conta.AccessTokenExpiraEm = null;
            conta.DataAtualizacao = DateTime.UtcNow;
            await repository.SalvarAsync(cancellationToken);
            throw;
        }

        conta.AccessTokenProtegido = protector.Protect(token.AccessToken);
        conta.AccessTokenExpiraEm = DateTime.UtcNow.AddSeconds(Math.Max(60, token.ExpiresIn));
        conta.DataAtualizacao = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(token.RefreshToken))
        {
            conta.RefreshTokenProtegido = protector.Protect(token.RefreshToken);
        }

        await repository.SalvarAsync(cancellationToken);
        return token.AccessToken;
    }
}
