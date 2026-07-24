using System.Diagnostics;
using System.Security.Cryptography;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Services;

public sealed class GoogleAdsConnectionService(
    IConfigurationResolver resolver,
    IGoogleAdsContaRepository repository,
    IGoogleAdsOAuthClient oauthClient,
    IGoogleAdsTokenService tokenService,
    ISecretProtector protector) : IGoogleAdsConnectionService
{
    public async Task<GoogleAdsStatusResponse> ObterStatusAsync(CancellationToken cancellationToken)
    {
        var conta = await repository.ObterPadraoAsync(cancellationToken);
        if (conta is null)
        {
            return new GoogleAdsStatusResponse(false, "Nao conectado", null, null, null);
        }

        var expirado = conta.AccessTokenExpiraEm is not null && conta.AccessTokenExpiraEm.Value <= DateTime.UtcNow;
        return new GoogleAdsStatusResponse(true, expirado ? "Token expirado" : "Conectado", conta.Id, conta.CustomerId, conta.Nome);
    }

    public async Task<GoogleAdsAuthUrlResponse> GerarAuthUrlAsync(CancellationToken cancellationToken)
    {
        var config = await Config(cancellationToken);
        if (!config.OAuthConfigurado)
        {
            throw new InvalidOperationException("Configure ClientId, ClientSecret e RedirectUri do Google Ads antes de conectar.");
        }

        var state = NewState();
        var url = AddQueryString(config.AuthEndpoint, new Dictionary<string, string?>
        {
            ["client_id"] = config.ClientId,
            ["redirect_uri"] = config.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = config.Scopes,
            ["access_type"] = "offline",
            ["prompt"] = "consent",
            ["state"] = state
        });

        return new GoogleAdsAuthUrlResponse(url, state);
    }

    public async Task<IReadOnlyList<GoogleAdsContaResponse>> ConcluirOAuthAsync(GoogleAdsOAuthCallbackRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new ArgumentException("Codigo OAuth obrigatorio.");
        }

        var config = await Config(cancellationToken);
        if (!config.OAuthConfigurado)
        {
            throw new InvalidOperationException("Configuracao OAuth Google Ads incompleta.");
        }

        var redirectUri = string.IsNullOrWhiteSpace(request.RedirectUri) ? config.RedirectUri : request.RedirectUri;
        var token = await oauthClient.ExchangeCodeAsync(request.Code, redirectUri, cancellationToken);
        var user = await oauthClient.GetUserInfoAsync(token.AccessToken, cancellationToken);
        var accounts = await oauthClient.ListAccessibleAccountsAsync(token.AccessToken, cancellationToken);
        if (accounts.Count == 0)
        {
            throw new InvalidOperationException("Nenhuma conta Google Ads acessivel foi encontrada.");
        }

        var existing = await repository.ListarAsync(cancellationToken);
        var hasDefault = existing.Any(x => x.Padrao);
        foreach (var account in accounts)
        {
            var customerId = NormalizeCustomerId(account.CustomerId);
            var conta = await repository.ObterPorCustomerIdAsync(customerId, cancellationToken);
            if (conta is null)
            {
                conta = new GoogleAdsConta
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    DataConexao = DateTime.UtcNow,
                    Ativa = true,
                    Padrao = !hasDefault
                };
                hasDefault = true;
                await repository.AdicionarAsync(conta, cancellationToken);
            }

            conta.Nome = string.IsNullOrWhiteSpace(account.Nome) ? $"Google Ads {customerId}" : account.Nome;
            conta.Email = user.Email;
            conta.Ativa = true;
            conta.DataAtualizacao = DateTime.UtcNow;
            conta.AccessTokenProtegido = protector.Protect(token.AccessToken);
            conta.AccessTokenExpiraEm = DateTime.UtcNow.AddSeconds(Math.Max(60, token.ExpiresIn));
            if (!string.IsNullOrWhiteSpace(token.RefreshToken))
            {
                conta.RefreshTokenProtegido = protector.Protect(token.RefreshToken);
            }
        }

        await repository.SalvarAsync(cancellationToken);
        return await ListarContasAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GoogleAdsContaResponse>> ListarContasAsync(CancellationToken cancellationToken)
    {
        var contas = await repository.ListarAsync(cancellationToken);
        return contas.Select(Map).ToArray();
    }

    public async Task<GoogleAdsContaResponse> SelecionarContaPadraoAsync(Guid id, CancellationToken cancellationToken)
    {
        var contas = await repository.ListarAsync(cancellationToken);
        var selecionada = contas.FirstOrDefault(x => x.Id == id && x.Ativa);
        if (selecionada is null)
        {
            throw new InvalidOperationException("Conta Google Ads nao encontrada.");
        }

        foreach (var conta in contas)
        {
            conta.Padrao = conta.Id == id;
            conta.DataAtualizacao = DateTime.UtcNow;
        }

        await repository.SalvarAsync(cancellationToken);
        return Map(selecionada);
    }

    public async Task<GoogleAdsTestarResponse> TestarAsync(GoogleAdsTestarRequest request, CancellationToken cancellationToken)
    {
        var conta = request.ContaId is null
            ? await repository.ObterPadraoAsync(cancellationToken)
            : await repository.ObterPorIdAsync(request.ContaId.Value, cancellationToken);
        if (conta is null || !conta.Ativa)
        {
            return new GoogleAdsTestarResponse(false, "Conta Google Ads nao encontrada.");
        }

        var config = await Config(cancellationToken);
        if (!config.ApiConfigurada)
        {
            return new GoogleAdsTestarResponse(false, "Configuracao Google Ads incompleta.", conta.CustomerId);
        }

        var sw = Stopwatch.StartNew();
        var accessToken = await tokenService.ObterAccessTokenValidoAsync(conta, cancellationToken);
        await oauthClient.TestConnectionAsync(accessToken, conta.CustomerId, cancellationToken);
        sw.Stop();
        return new GoogleAdsTestarResponse(true, "Conexao Google Ads valida.", conta.CustomerId, sw.ElapsedMilliseconds);
    }

    private async Task<GoogleAdsConfiguration> Config(CancellationToken cancellationToken)
    {
        return new GoogleAdsConfiguration(
            await Value("ClientId", cancellationToken),
            await Value("ClientSecret", cancellationToken),
            await Value("DeveloperToken", cancellationToken),
            await Value("LoginCustomerId", cancellationToken),
            await Value("RedirectUri", cancellationToken) ?? string.Empty,
            await Value("AuthEndpoint", cancellationToken) ?? string.Empty,
            await Value("TokenEndpoint", cancellationToken) ?? string.Empty,
            await Value("UserInfoEndpoint", cancellationToken) ?? string.Empty,
            await Value("ApiBaseUrl", cancellationToken) ?? string.Empty,
            await Value("Scopes", cancellationToken) ?? string.Empty);
    }

    private async Task<string?> Value(string key, CancellationToken cancellationToken)
    {
        return (await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, key, cancellationToken)).Value;
    }

    private static GoogleAdsContaResponse Map(GoogleAdsConta conta)
    {
        return new GoogleAdsContaResponse(conta.Id, conta.CustomerId, conta.Nome, conta.Email, conta.Ativa, conta.Padrao, conta.DataConexao, conta.AccessTokenExpiraEm);
    }

    private static string NormalizeCustomerId(string customerId)
    {
        return new string(customerId.Where(char.IsDigit).ToArray());
    }

    private static string NewState()
    {
        Span<byte> bytes = stackalloc byte[24];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string AddQueryString(string url, Dictionary<string, string?> values)
    {
        var separator = url.Contains('?') ? "&" : "?";
        return url + separator + string.Join("&", values
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value!)}"));
    }
}
