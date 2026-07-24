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
    IGoogleAdsOAuthStateRepository stateRepository,
    IGoogleAdsOAuthClient oauthClient,
    IGoogleAdsTokenService tokenService,
    ISecretProtector protector) : IGoogleAdsConnectionService
{
    public async Task<GoogleAdsStatusResponse> ObterStatusAsync(CancellationToken cancellationToken)
    {
        var config = await Config(cancellationToken);
        if (!config.OAuthConfigurado)
        {
            return new GoogleAdsStatusResponse(false, "Nao configurado", null, null, null);
        }

        var conta = await repository.ObterPadraoAsync(cancellationToken);
        if (conta is null)
        {
            var contas = await repository.ListarAsync(cancellationToken);
            return contas.Any(x => x.Ativa)
                ? new GoogleAdsStatusResponse(true, "Conectado sem conta padrao", null, null, null)
                : new GoogleAdsStatusResponse(false, "Configurado, mas OAuth nao conectado", null, null, null);
        }

        var expirado = conta.AccessTokenExpiraEm is not null && conta.AccessTokenExpiraEm.Value <= DateTime.UtcNow;
        return new GoogleAdsStatusResponse(true, expirado ? "Token expirado ou invalido" : "Conectado", conta.Id, GoogleAdsCustomerId.Mask(conta.CustomerId), conta.Nome);
    }

    public async Task<GoogleAdsAuthUrlResponse> GerarAuthUrlAsync(CancellationToken cancellationToken)
    {
        var config = await Config(cancellationToken);
        if (!config.OAuthConfigurado)
        {
            throw new InvalidOperationException("Configure ClientId, ClientSecret e RedirectUri do Google Ads antes de conectar.");
        }

        var state = NewState();
        await stateRepository.AdicionarAsync(new GoogleAdsOAuthState
        {
            Id = Guid.NewGuid(),
            StateHash = Hash(state),
            ExpiraEm = DateTime.UtcNow.AddMinutes(10),
            Utilizado = false,
            DataCriacao = DateTime.UtcNow
        }, cancellationToken);
        await stateRepository.SalvarAsync(cancellationToken);
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

    public async Task<GoogleAdsAmbienteResponse> ObterAmbienteAsync(CancellationToken cancellationToken)
    {
        var conta = await repository.ObterPadraoAsync(cancellationToken);
        var useTest = bool.TryParse(await Value("UseTestAccount", cancellationToken), out var test) && test;
        var enableRealPublishing = bool.TryParse(await Value("EnableRealPublishing", cancellationToken), out var enabled) && enabled;
        var testCustomer = await Value("TestCustomerId", cancellationToken);
        var pendencias = new List<string>();
        var normalizedConta = GoogleAdsCustomerId.TryNormalize(conta?.CustomerId, out var contaCustomer) ? contaCustomer : null;
        var normalizedTest = GoogleAdsCustomerId.TryNormalize(testCustomer, out var testCustomerNormalized) ? testCustomerNormalized : null;

        if (!useTest) pendencias.Add("UseTestAccount precisa estar true nesta fase.");
        if (!enableRealPublishing) pendencias.Add("EnableRealPublishing esta desabilitado.");
        if (useTest && normalizedTest is null) pendencias.Add("TestCustomerId obrigatorio em modo teste.");
        if (conta is null || !conta.Ativa) pendencias.Add("Conta Google Ads padrao nao selecionada.");
        if (useTest && normalizedConta is not null && normalizedTest is not null && normalizedConta != normalizedTest) pendencias.Add("Conta selecionada difere do TestCustomerId.");

        return new GoogleAdsAmbienteResponse(
            useTest ? "Teste" : "ProducaoBloqueada",
            GoogleAdsCustomerId.Mask(normalizedTest ?? normalizedConta),
            normalizedConta is not null && (!useTest || normalizedConta == normalizedTest),
            useTest && enableRealPublishing && normalizedConta is not null && normalizedTest is not null && normalizedConta == normalizedTest,
            pendencias);
    }

    public async Task<GoogleAdsOAuthCallbackResponse> ConcluirOAuthAsync(GoogleAdsOAuthCallbackRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new ArgumentException("Codigo OAuth obrigatorio.");
        }
        if (string.IsNullOrWhiteSpace(request.State))
        {
            throw new ArgumentException("State OAuth obrigatorio.");
        }

        var config = await Config(cancellationToken);
        if (!config.OAuthConfigurado)
        {
            throw new InvalidOperationException("Configuracao OAuth Google Ads incompleta.");
        }

        await ValidateStateAsync(request.State, cancellationToken);
        var redirectUri = string.IsNullOrWhiteSpace(request.RedirectUri) ? config.RedirectUri : request.RedirectUri;
        var token = await oauthClient.ExchangeCodeAsync(request.Code, redirectUri, cancellationToken);
        if (string.IsNullOrWhiteSpace(token.RefreshToken))
        {
            throw new InvalidOperationException("Google nao retornou refresh token. Revogue o acesso do app no Google e conecte novamente com consentimento.");
        }

        var user = await oauthClient.GetUserInfoAsync(token.AccessToken, cancellationToken);
        var accounts = await oauthClient.ListAccessibleAccountsAsync(token.AccessToken, cancellationToken);
        if (accounts.Count == 0)
        {
            throw new InvalidOperationException("Nenhuma conta Google Ads acessivel foi encontrada.");
        }

        foreach (var account in accounts)
        {
            var customerId = GoogleAdsCustomerId.Normalize(account.CustomerId);
            var conta = await repository.ObterPorCustomerIdAsync(customerId, cancellationToken);
            if (conta is null)
            {
                conta = new GoogleAdsConta
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    DataConexao = DateTime.UtcNow,
                    Ativa = true,
                    Padrao = false
                };
                await repository.AdicionarAsync(conta, cancellationToken);
            }

            conta.Nome = string.IsNullOrWhiteSpace(account.Nome) ? $"Google Ads {customerId}" : account.Nome;
            conta.Email = user.Email;
            conta.Ativa = true;
            conta.DataAtualizacao = DateTime.UtcNow;
            conta.AccessTokenProtegido = protector.Protect(token.AccessToken);
            conta.AccessTokenExpiraEm = DateTime.UtcNow.AddSeconds(Math.Max(60, token.ExpiresIn));
            conta.RefreshTokenProtegido = protector.Protect(token.RefreshToken);
        }

        await repository.SalvarAsync(cancellationToken);
        var contas = await ListarContasAsync(cancellationToken);
        return new GoogleAdsOAuthCallbackResponse(true, true, contas.Count, "Google Ads conectado com sucesso.", contas);
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
            return new GoogleAdsTestarResponse(false, "Conta Google Ads nao encontrada.", Pendencias: ["Conta Google Ads padrao ausente."]);
        }

        var config = await Config(cancellationToken);
        if (!config.ApiConfigurada)
        {
            return new GoogleAdsTestarResponse(false, "Configuracao Google Ads incompleta.", conta.CustomerId, CustomerIdMascarado: GoogleAdsCustomerId.Mask(conta.CustomerId), Pendencias: ["ClientId, ClientSecret, RedirectUri ou DeveloperToken pendente."]);
        }

        var sw = Stopwatch.StartNew();
        var beforeExpiration = conta.AccessTokenExpiraEm;
        var accessToken = await tokenService.ObterAccessTokenValidoAsync(conta, cancellationToken);
        await oauthClient.TestConnectionAsync(accessToken, conta.CustomerId, cancellationToken);
        sw.Stop();
        var ambiente = await ObterAmbienteAsync(cancellationToken);
        return new GoogleAdsTestarResponse(
            true,
            "Conexao Google Ads valida.",
            conta.CustomerId,
            sw.ElapsedMilliseconds,
            ambiente.Modo,
            GoogleAdsCustomerId.Mask(conta.CustomerId),
            beforeExpiration != conta.AccessTokenExpiraEm,
            true,
            true,
            ambiente.Pendencias);
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
        return new GoogleAdsContaResponse(conta.Id, conta.CustomerId, GoogleAdsCustomerId.Mask(conta.CustomerId), conta.Nome, conta.Email, conta.Ativa, conta.Padrao, "Cliente", false, conta.DataConexao, conta.AccessTokenExpiraEm);
    }

    private async Task ValidateStateAsync(string state, CancellationToken cancellationToken)
    {
        var hash = Hash(state);
        var stored = await stateRepository.ObterPorHashAsync(hash, cancellationToken);
        if (stored is null)
        {
            throw new ArgumentException("State OAuth invalido.");
        }
        if (stored.Utilizado)
        {
            throw new ArgumentException("Callback OAuth ja utilizado.");
        }
        if (stored.ExpiraEm <= DateTime.UtcNow)
        {
            throw new ArgumentException("State OAuth expirado.");
        }

        stored.Utilizado = true;
        stored.DataUtilizacao = DateTime.UtcNow;
        await stateRepository.SalvarAsync(cancellationToken);
    }

    private static string NewState()
    {
        Span<byte> bytes = stackalloc byte[24];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string Hash(string value)
    {
        return Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
    }

    private static string AddQueryString(string url, Dictionary<string, string?> values)
    {
        var separator = url.Contains('?') ? "&" : "?";
        return url + separator + string.Join("&", values
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value!)}"));
    }
}
