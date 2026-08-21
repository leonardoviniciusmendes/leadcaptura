using System.Security.Cryptography;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Services;

public sealed class MetaAdsConnectionService(
    IConfigurationResolver resolver,
    IMetaAdsContaRepository contaRepository,
    IMetaAdsAtivoSelecionadoRepository selecaoRepository,
    IMetaAdsOAuthStateRepository stateRepository,
    IMetaAdsOAuthClient oauthClient,
    ISecretProtector protector) : IMetaAdsConnectionService
{
    public async Task<MetaAdsStatusResponse> ObterStatusAsync(CancellationToken cancellationToken)
    {
        var config = await Config(cancellationToken);
        var conta = await contaRepository.ObterAtivaAsync(cancellationToken);
        if (!config.OAuthConfigurado)
        {
            return new MetaAdsStatusResponse(false, false, false, "Nao configurado");
        }

        if (conta is null)
        {
            return new MetaAdsStatusResponse(true, false, false, "Configurado, mas OAuth nao conectado");
        }

        if (string.IsNullOrWhiteSpace(conta.AccessTokenProtegido))
        {
            return new MetaAdsStatusResponse(true, false, false, "Reconexao necessaria", conta.Id, conta.MetaUserId, conta.Nome, conta.DataConexao, conta.AccessTokenExpiraEm);
        }

        var selecao = await selecaoRepository.ObterPorContaIdAsync(conta.Id, cancellationToken);
        return new MetaAdsStatusResponse(true, true, !string.IsNullOrWhiteSpace(selecao?.AdAccountId), "Conectado", conta.Id, conta.MetaUserId, conta.Nome, conta.DataConexao, conta.AccessTokenExpiraEm);
    }

    public async Task<MetaAdsAuthUrlResponse> GerarAuthUrlAsync(CancellationToken cancellationToken)
    {
        return await GerarAuthUrlAsync(false, cancellationToken);
    }

    public async Task<MetaAdsAuthUrlResponse> GerarAuthUrlAsync(bool incluirPermissaoPublicacao, CancellationToken cancellationToken)
    {
        var config = await Config(cancellationToken);
        if (!config.OAuthConfigurado)
        {
            throw new InvalidOperationException("Configure AppId, AppSecret e RedirectUri do Meta Ads antes de conectar.");
        }

        var state = NewState();
        await stateRepository.AdicionarAsync(new MetaAdsOAuthState
        {
            Id = Guid.NewGuid(),
            StateHash = Hash(state),
            ExpiraEm = DateTime.UtcNow.AddMinutes(10),
            Utilizado = false,
            DataCriacao = DateTime.UtcNow
        }, cancellationToken);
        await stateRepository.SalvarAsync(cancellationToken);

        if (incluirPermissaoPublicacao)
        {
            config = config with { Scopes = NormalizeScopes(config.Scopes, true) };
        }

        return new MetaAdsAuthUrlResponse(oauthClient.BuildAuthorizationUrl(config, state), state);
    }

    public async Task<MetaAdsOAuthCallbackResponse> ConcluirOAuthAsync(MetaAdsOAuthCallbackRequest request, CancellationToken cancellationToken)
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
            throw new InvalidOperationException("Configuracao OAuth Meta Ads incompleta.");
        }

        await ValidateStateAsync(request.State, cancellationToken);
        var token = await oauthClient.ExchangeCodeAsync(request.Code, cancellationToken);
        if (string.IsNullOrWhiteSpace(token.AccessToken))
        {
            throw new InvalidOperationException("Resposta OAuth Meta sem access token.");
        }

        var user = await oauthClient.GetUserInfoAsync(token.AccessToken, cancellationToken);
        var conta = await contaRepository.ObterPorMetaUserIdAsync(user.Id, cancellationToken);
        var now = DateTime.UtcNow;
        if (conta is null)
        {
            conta = new MetaAdsConta
            {
                Id = Guid.NewGuid(),
                MetaUserId = user.Id,
                DataConexao = now
            };
            await contaRepository.AdicionarAsync(conta, cancellationToken);
        }

        conta.Nome = user.Name;
        conta.Ativa = true;
        conta.DataAtualizacao = now;
        conta.AccessTokenProtegido = protector.Protect(token.AccessToken);
        conta.TokenType = token.TokenType;
        conta.AccessTokenExpiraEm = token.ExpiresIn is > 0 ? now.AddSeconds(token.ExpiresIn.Value) : null;
        await contaRepository.SalvarAsync(cancellationToken);

        var status = await ObterStatusAsync(cancellationToken);
        return new MetaAdsOAuthCallbackResponse(true, true, "Meta Ads conectado com sucesso.", status);
    }

    public async Task<MetaAdsStatusResponse> DesconectarAsync(CancellationToken cancellationToken)
    {
        var conta = await contaRepository.ObterAtivaAsync(cancellationToken);
        if (conta is not null)
        {
            conta.Ativa = false;
            conta.AccessTokenProtegido = null;
            conta.AccessTokenExpiraEm = null;
            conta.TokenType = null;
            conta.DataAtualizacao = DateTime.UtcNow;
            await contaRepository.SalvarAsync(cancellationToken);
        }

        return await ObterStatusAsync(cancellationToken);
    }

    private async Task<MetaAdsConfiguration> Config(CancellationToken cancellationToken)
    {
        return new MetaAdsConfiguration(
            await Value("AppId", cancellationToken),
            await Value("AppSecret", cancellationToken),
            await Value("RedirectUri", cancellationToken) ?? string.Empty,
            await Value("AuthEndpoint", cancellationToken) ?? string.Empty,
            await Value("TokenEndpoint", cancellationToken) ?? string.Empty,
            await Value("UserInfoEndpoint", cancellationToken) ?? string.Empty,
            await Value("GraphApiBaseUrl", cancellationToken) ?? string.Empty,
            await Value("GraphApiVersion", cancellationToken) ?? string.Empty,
            NormalizeScopes(await Value("Scopes", cancellationToken), false));
    }

    private async Task<string?> Value(string key, CancellationToken cancellationToken)
    {
        return (await resolver.ResolveAsync(CategoriaConfiguracao.MetaAds, key, cancellationToken)).Value;
    }

    private static string NormalizeScopes(string? configured, bool incluirPermissaoPublicacao)
    {
        var scopes = new HashSet<string>((configured ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), StringComparer.OrdinalIgnoreCase)
        {
            "public_profile",
            "business_management",
            "ads_read",
            "pages_show_list",
            "pages_read_engagement",
            "instagram_basic"
        };
        if (incluirPermissaoPublicacao)
        {
            scopes.Add("ads_management");
        }
        return string.Join(' ', scopes);
    }

    private async Task ValidateStateAsync(string state, CancellationToken cancellationToken)
    {
        var stored = await stateRepository.ObterPorHashAsync(Hash(state), cancellationToken);
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
}
