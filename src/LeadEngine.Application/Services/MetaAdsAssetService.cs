using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Services;

public sealed class MetaAdsAssetService(
    IConfigurationResolver resolver,
    IMetaAdsContaRepository contaRepository,
    IMetaAdsAtivoSelecionadoRepository selecaoRepository,
    IMetaAdsGraphClient graphClient,
    ISecretProtector protector) : IMetaAdsAssetService
{
    public async Task<MetaAdsAssetListResponse<MetaAdsBusinessResponse>> ListarBusinessesAsync(CancellationToken cancellationToken)
    {
        return await ListAsync(async (config, token) =>
        {
            var items = await graphClient.ListBusinessesAsync(config, token, cancellationToken);
            return new MetaAdsAssetListResponse<MetaAdsBusinessResponse>(true, items, items.Count == 0 ? "Nenhum Business encontrado." : null);
        }, cancellationToken);
    }

    public async Task<MetaAdsAssetListResponse<MetaAdsAdAccountResponse>> ListarAdAccountsAsync(string businessId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(businessId))
        {
            throw new ArgumentException("Business obrigatorio.");
        }

        return await ListAsync(async (config, token) =>
        {
            var items = await graphClient.ListAdAccountsAsync(config, token, businessId, cancellationToken);
            return new MetaAdsAssetListResponse<MetaAdsAdAccountResponse>(true, items, items.Count == 0 ? "Nenhuma conta de anuncios encontrada para este Business." : null);
        }, cancellationToken);
    }

    public async Task<MetaAdsAssetListResponse<MetaAdsPageResponse>> ListarPagesAsync(CancellationToken cancellationToken)
    {
        return await ListAsync(async (config, token) =>
        {
            var items = await graphClient.ListPagesAsync(config, token, cancellationToken);
            return new MetaAdsAssetListResponse<MetaAdsPageResponse>(true, items, items.Count == 0 ? "Nenhuma Page encontrada." : null);
        }, cancellationToken);
    }

    public async Task<MetaAdsAssetListResponse<MetaAdsInstagramAccountResponse>> ObterInstagramAsync(string pageId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(pageId))
        {
            throw new ArgumentException("Page obrigatoria.");
        }

        return await ListAsync(async (config, token) =>
        {
            var page = await graphClient.GetPageAsync(config, token, pageId, cancellationToken);
            if (page is null)
            {
                return new MetaAdsAssetListResponse<MetaAdsInstagramAccountResponse>(false, [], "Page nao encontrada.");
            }

            return page.Instagram is null
                ? new MetaAdsAssetListResponse<MetaAdsInstagramAccountResponse>(true, [], "Instagram Professional Account nao vinculado a esta Page.")
                : new MetaAdsAssetListResponse<MetaAdsInstagramAccountResponse>(true, [page.Instagram]);
        }, cancellationToken);
    }

    public async Task<MetaAdsAssetListResponse<MetaAdsPixelResponse>> ListarPixelsAsync(string adAccountId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(adAccountId))
        {
            throw new ArgumentException("Ad Account obrigatoria.");
        }

        return await ListAsync(async (config, token) =>
        {
            var items = await graphClient.ListPixelsAsync(config, token, adAccountId, cancellationToken);
            return new MetaAdsAssetListResponse<MetaAdsPixelResponse>(true, items, items.Count == 0 ? "Nenhum Pixel/Dataset encontrado para esta Ad Account." : null);
        }, cancellationToken);
    }

    public async Task<MetaAdsAssetSelectionResponse> ObterSelecaoAsync(CancellationToken cancellationToken)
    {
        var conta = await ContaAtivaAsync(cancellationToken);
        var selecao = await selecaoRepository.ObterPorContaIdAsync(conta.Id, cancellationToken);
        return ToResponse(selecao);
    }

    public async Task<MetaAdsAssetSelectionResponse> SalvarSelecaoAsync(MetaAdsAssetSelectionRequest request, CancellationToken cancellationToken)
    {
        var (conta, token, config) = await ContextAsync(cancellationToken);
        MetaAdsBusinessResponse? business = null;
        MetaAdsAdAccountResponse? adAccount = null;
        MetaAdsPageResponse? page = null;
        MetaAdsPixelResponse? pixel = null;

        if (!string.IsNullOrWhiteSpace(request.BusinessId))
        {
            business = (await graphClient.ListBusinessesAsync(config, token, cancellationToken))
                .FirstOrDefault(x => Same(x.Id, request.BusinessId));
            if (business is null) throw new InvalidOperationException("Business informado nao pertence ao usuario Meta conectado.");
        }

        if (!string.IsNullOrWhiteSpace(request.AdAccountId))
        {
            if (business is null) throw new ArgumentException("Selecione um Business antes da Ad Account.");
            adAccount = (await graphClient.ListAdAccountsAsync(config, token, business.Id, cancellationToken))
                .FirstOrDefault(x => Same(x.Id, request.AdAccountId) || Same(x.AccountId, request.AdAccountId) || Same(x.Id, NormalizeAdAccount(request.AdAccountId)));
            if (adAccount is null) throw new InvalidOperationException("Ad Account informada nao pertence ao Business selecionado.");
        }

        if (!string.IsNullOrWhiteSpace(request.PageId))
        {
            page = (await graphClient.ListPagesAsync(config, token, cancellationToken))
                .FirstOrDefault(x => Same(x.Id, request.PageId));
            if (page is null) throw new InvalidOperationException("Page informada nao pertence ao usuario Meta conectado.");
        }

        if (!string.IsNullOrWhiteSpace(request.PixelId))
        {
            if (adAccount is null) throw new ArgumentException("Selecione uma Ad Account antes do Pixel/Dataset.");
            pixel = (await graphClient.ListPixelsAsync(config, token, adAccount.Id, cancellationToken))
                .FirstOrDefault(x => Same(x.Id, request.PixelId));
            if (pixel is null) throw new InvalidOperationException("Pixel/Dataset informado nao pertence a Ad Account selecionada.");
        }

        var selecao = await selecaoRepository.ObterPorContaIdAsync(conta.Id, cancellationToken);
        var now = DateTime.UtcNow;
        if (selecao is null)
        {
            selecao = new MetaAdsAtivoSelecionado
            {
                Id = Guid.NewGuid(),
                MetaAdsContaId = conta.Id,
                DataCriacao = now
            };
            await selecaoRepository.AdicionarAsync(selecao, cancellationToken);
        }

        selecao.BusinessId = business?.Id;
        selecao.BusinessNome = business?.Nome;
        selecao.AdAccountId = adAccount?.Id;
        selecao.AdAccountNome = adAccount?.Nome;
        selecao.PageId = page?.Id;
        selecao.PageNome = page?.Nome;
        selecao.InstagramAccountId = page?.Instagram?.Id;
        selecao.InstagramNome = page?.Instagram?.Username ?? page?.Instagram?.Nome;
        selecao.PixelId = pixel?.Id;
        selecao.PixelNome = pixel?.Nome;
        selecao.DataAtualizacao = now;
        await selecaoRepository.SalvarAsync(cancellationToken);
        return ToResponse(selecao);
    }

    private async Task<MetaAdsAssetListResponse<T>> ListAsync<T>(Func<MetaAdsConfiguration, string, Task<MetaAdsAssetListResponse<T>>> action, CancellationToken cancellationToken)
    {
        try
        {
            var (_, token, config) = await ContextAsync(cancellationToken);
            return await action(config, token);
        }
        catch (MetaAdsGraphApiException ex) when (ex.PermissionRequired)
        {
            return new MetaAdsAssetListResponse<T>(false, [], "Permissao Meta insuficiente. Reconecte Meta Ads autorizando as permissoes solicitadas.", true);
        }
        catch (MetaAdsGraphApiException ex)
        {
            return new MetaAdsAssetListResponse<T>(false, [], ex.Message);
        }
    }

    private async Task<(MetaAdsConta Conta, string AccessToken, MetaAdsConfiguration Config)> ContextAsync(CancellationToken cancellationToken)
    {
        var conta = await ContaAtivaAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(conta.AccessTokenProtegido))
        {
            throw new InvalidOperationException("Reconecte Meta Ads antes de listar ativos.");
        }
        if (conta.AccessTokenExpiraEm is not null && conta.AccessTokenExpiraEm <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Token Meta expirado. Reconecte Meta Ads.");
        }

        return (conta, protector.Unprotect(conta.AccessTokenProtegido), await Config(cancellationToken));
    }

    private async Task<MetaAdsConta> ContaAtivaAsync(CancellationToken cancellationToken)
    {
        return await contaRepository.ObterAtivaAsync(cancellationToken)
            ?? throw new InvalidOperationException("Meta Ads nao conectado.");
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
            await Value("Scopes", cancellationToken) ?? string.Empty);
    }

    private async Task<string?> Value(string key, CancellationToken cancellationToken)
    {
        return (await resolver.ResolveAsync(CategoriaConfiguracao.MetaAds, key, cancellationToken)).Value;
    }

    private static MetaAdsAssetSelectionResponse ToResponse(MetaAdsAtivoSelecionado? selecao)
    {
        return selecao is null
            ? new MetaAdsAssetSelectionResponse(null, null, null, null, null, null, null, null, null, null, null, null, null)
            : new MetaAdsAssetSelectionResponse(selecao.Id, selecao.MetaAdsContaId, selecao.BusinessId, selecao.BusinessNome, selecao.AdAccountId, selecao.AdAccountNome, selecao.PageId, selecao.PageNome, selecao.InstagramAccountId, selecao.InstagramNome, selecao.PixelId, selecao.PixelNome, selecao.DataAtualizacao);
    }

    private static bool Same(string? left, string? right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeAdAccount(string value)
    {
        return value.StartsWith("act_", StringComparison.OrdinalIgnoreCase) ? value : $"act_{value}";
    }
}
