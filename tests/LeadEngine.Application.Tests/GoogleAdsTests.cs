using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Tests;

public sealed class GoogleAdsTests
{
    [Fact]
    public async Task OAuth_CriaContaEProtegeTokens()
    {
        var repo = new Repo();
        var protector = new Protector();
        var oauth = new OAuth();
        var service = Service(repo, oauth, protector);
        var auth = await service.GerarAuthUrlAsync(CancellationToken.None);

        var callback = await service.ConcluirOAuthAsync(new GoogleAdsOAuthCallbackRequest("code", auth.State, null), CancellationToken.None);

        Assert.True(callback.Sucesso);
        var conta = Assert.Single(callback.Contas);
        Assert.Equal("1234567890", conta.CustomerId);
        var saved = Assert.Single(repo.Contas);
        Assert.NotEqual("access-token", saved.AccessTokenProtegido);
        Assert.NotEqual("refresh-token", saved.RefreshTokenProtegido);
        Assert.Equal("protected:access-token", saved.AccessTokenProtegido);
        Assert.False(saved.Padrao);
    }

    [Fact]
    public async Task TokenService_RenovaAccessTokenExpirado()
    {
        var repo = new Repo();
        var conta = Conta();
        conta.AccessTokenExpiraEm = DateTime.UtcNow.AddMinutes(-5);
        repo.Contas.Add(conta);
        var oauth = new OAuth { RefreshResult = new GoogleAdsTokenResult("new-access", null, 3600, null, "Bearer") };
        var tokenService = new GoogleAdsTokenService(oauth, new Protector(), repo);

        var token = await tokenService.ObterAccessTokenValidoAsync(conta, CancellationToken.None);

        Assert.Equal("new-access", token);
        Assert.Equal("protected:new-access", conta.AccessTokenProtegido);
        Assert.Equal(1, oauth.RefreshCalls);
    }

    [Fact]
    public async Task TestarConexao_UsaContaPadrao()
    {
        var repo = new Repo();
        repo.Contas.Add(Conta());
        var oauth = new OAuth();
        var service = Service(repo, oauth, new Protector());

        var result = await service.TestarAsync(new GoogleAdsTestarRequest(), CancellationToken.None);

        Assert.True(result.Sucesso);
        Assert.Equal("1234567890", result.CustomerId);
        Assert.Equal(1, oauth.TestCalls);
    }

    [Fact]
    public async Task OAuth_FalhaAutenticacaoNaoCriaConta()
    {
        var repo = new Repo();
        var oauth = new OAuth { FailExchange = true };
        var service = Service(repo, oauth, new Protector());
        var auth = await service.GerarAuthUrlAsync(CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConcluirOAuthAsync(new GoogleAdsOAuthCallbackRequest("code", auth.State, null), CancellationToken.None));
        Assert.Empty(repo.Contas);
    }

    [Fact]
    public async Task OAuth_StateInvalidoOuReutilizadoBloqueia()
    {
        var service = Service(new Repo(), new OAuth(), new Protector());
        var auth = await service.GerarAuthUrlAsync(CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentException>(() => service.ConcluirOAuthAsync(new GoogleAdsOAuthCallbackRequest("code", "state-invalido", null), CancellationToken.None));
        await service.ConcluirOAuthAsync(new GoogleAdsOAuthCallbackRequest("code", auth.State, null), CancellationToken.None);
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.ConcluirOAuthAsync(new GoogleAdsOAuthCallbackRequest("code", auth.State, null), CancellationToken.None));

        Assert.Contains("ja utilizado", ex.Message);
    }

    [Fact]
    public async Task Status_ConectadoSemContaPadrao()
    {
        var repo = new Repo();
        repo.Contas.Add(Conta(padrao: false));
        var result = await Service(repo, new OAuth(), new Protector()).ObterStatusAsync(CancellationToken.None);

        Assert.True(result.Conectado);
        Assert.Equal("Conectado sem conta padrao", result.Status);
        Assert.Null(result.ContaPadraoId);
    }

    [Fact]
    public async Task TestarConexao_ContaNaoEncontrada()
    {
        var result = await Service(new Repo(), new OAuth(), new Protector()).TestarAsync(new GoogleAdsTestarRequest(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Sucesso);
        Assert.Contains("nao encontrada", result.Status);
    }

    [Fact]
    public async Task AuthUrl_IncluiEscopoEState()
    {
        var result = await Service(new Repo(), new OAuth(), new Protector()).GerarAuthUrlAsync(CancellationToken.None);

        Assert.Contains("client_id=client-id", result.Url);
        Assert.Contains("scope=", result.Url);
        Assert.False(string.IsNullOrWhiteSpace(result.State));
    }

    private static GoogleAdsConnectionService Service(Repo repo, OAuth oauth, Protector protector)
    {
        var resolver = new Resolver(new Dictionary<string, string?>
        {
            ["ClientId"] = "client-id",
            ["ClientSecret"] = "client-secret",
            ["DeveloperToken"] = "developer-token",
            ["RedirectUri"] = "http://localhost:5173/leadcaptura/configuracoes?googleAdsCallback=1",
            ["AuthEndpoint"] = "https://accounts.google.com/o/oauth2/v2/auth",
            ["TokenEndpoint"] = "https://oauth2.googleapis.com/token",
            ["UserInfoEndpoint"] = "https://openidconnect.googleapis.com/v1/userinfo",
            ["ApiBaseUrl"] = "https://googleads.googleapis.com/v19",
            ["Scopes"] = "https://www.googleapis.com/auth/adwords openid email profile"
        });
        var token = new GoogleAdsTokenService(oauth, protector, repo);
        return new GoogleAdsConnectionService(resolver, repo, new StateRepo(), oauth, token, protector);
    }

    private static GoogleAdsConta Conta(bool padrao = true)
    {
        return new GoogleAdsConta
        {
            Id = Guid.NewGuid(),
            CustomerId = "1234567890",
            Nome = "Conta teste",
            Email = "user@example.com",
            Ativa = true,
            Padrao = padrao,
            DataConexao = DateTime.UtcNow,
            AccessTokenProtegido = "protected:access-token",
            RefreshTokenProtegido = "protected:refresh-token",
            AccessTokenExpiraEm = DateTime.UtcNow.AddHours(1)
        };
    }

    private sealed class Protector : ISecretProtector
    {
        public string Protect(string value) => $"protected:{value}";
        public string Unprotect(string protectedValue) => protectedValue.Replace("protected:", string.Empty);
    }

    private sealed class Resolver(Dictionary<string, string?> values) : IConfigurationResolver
    {
        public Task<ResolvedConfigurationValue> ResolveAsync(CategoriaConfiguracao categoria, string chave, CancellationToken cancellationToken)
        {
            values.TryGetValue(chave, out var value);
            return Task.FromResult(new ResolvedConfigurationValue(value, !string.IsNullOrWhiteSpace(value), OrigemConfiguracao.Banco, chave.Contains("Secret") || chave.Contains("Token")));
        }

        public Task InvalidateAsync(CategoriaConfiguracao categoria, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class Repo : IGoogleAdsContaRepository
    {
        public List<GoogleAdsConta> Contas { get; } = [];
        public Task<GoogleAdsConta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Contas.FirstOrDefault(x => x.Id == id));
        public Task<GoogleAdsConta?> ObterPorCustomerIdAsync(string customerId, CancellationToken cancellationToken) => Task.FromResult(Contas.FirstOrDefault(x => x.CustomerId == customerId));
        public Task<GoogleAdsConta?> ObterPadraoAsync(CancellationToken cancellationToken) => Task.FromResult(Contas.FirstOrDefault(x => x.Padrao && x.Ativa));
        public Task<IReadOnlyList<GoogleAdsConta>> ListarAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<GoogleAdsConta>>(Contas);
        public Task AdicionarAsync(GoogleAdsConta conta, CancellationToken cancellationToken) { Contas.Add(conta); return Task.CompletedTask; }
        public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class StateRepo : IGoogleAdsOAuthStateRepository
    {
        private readonly List<GoogleAdsOAuthState> states = [];
        public Task AdicionarAsync(GoogleAdsOAuthState state, CancellationToken cancellationToken) { states.Add(state); return Task.CompletedTask; }
        public Task<GoogleAdsOAuthState?> ObterPorHashAsync(string stateHash, CancellationToken cancellationToken) => Task.FromResult(states.FirstOrDefault(x => x.StateHash == stateHash));
        public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class OAuth : IGoogleAdsOAuthClient
    {
        public bool FailExchange { get; init; }
        public int RefreshCalls { get; private set; }
        public int TestCalls { get; private set; }
        public GoogleAdsTokenResult RefreshResult { get; init; } = new("access-token", "refresh-token", 3600, null, "Bearer");

        public Task<GoogleAdsTokenResult> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken)
        {
            if (FailExchange) throw new InvalidOperationException("Falha na autenticacao Google Ads.");
            return Task.FromResult(new GoogleAdsTokenResult("access-token", "refresh-token", 3600, null, "Bearer"));
        }

        public Task<GoogleAdsTokenResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
        {
            RefreshCalls++;
            return Task.FromResult(RefreshResult);
        }

        public Task<GoogleAdsUserInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken)
        {
            return Task.FromResult(new GoogleAdsUserInfo("user@example.com", "User"));
        }

        public Task<IReadOnlyList<GoogleAdsAccessibleAccount>> ListAccessibleAccountsAsync(string accessToken, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<GoogleAdsAccessibleAccount>>([new("customers/1234567890", "Conta teste")]);
        }

        public Task TestConnectionAsync(string accessToken, string customerId, CancellationToken cancellationToken)
        {
            TestCalls++;
            return Task.CompletedTask;
        }
    }
}
