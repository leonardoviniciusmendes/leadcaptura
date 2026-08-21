using System.Text.Json;
using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Services;

public sealed class MetaAdsPreviewService(
    ICampanhaRepository campanhaRepository,
    IMetaAdsContaRepository contaRepository,
    IMetaAdsAtivoSelecionadoRepository selecaoRepository,
    IMetaAdsImagemRepository imagemRepository,
    IMetaAdsPreparacaoPublicacaoRepository preparacaoRepository,
    IMetaAdsGraphClient graphClient,
    IConfigurationResolver resolver,
    ISecretProtector protector,
    CampaignPublicUrlBuilder publicUrlBuilder) : IMetaAdsPreviewService
{
    private const string PlannedStatus = "PAUSED";
    private const string InitialObjective = "OUTCOME_TRAFFIC";
    private const string InitialOptimizationGoal = "LINK_CLICKS";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly HashSet<string> SupportedSpecialCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "NONE", "HOUSING", "EMPLOYMENT", "CREDIT"
    };

    public async Task<MetaAdsPreviewResponse> GerarAsync(MetaAdsPreviewRequest request, CancellationToken cancellationToken)
    {
        var items = new List<MetaAdsPreflightItem>();
        var campanha = await campanhaRepository.ObterPorIdAsync(request.CampanhaId, cancellationToken)
            ?? throw new KeyNotFoundException("Campanha nao encontrada.");
        var conta = await contaRepository.ObterAtivaAsync(cancellationToken);
        var selecao = conta is null ? null : await selecaoRepository.ObterPorContaIdAsync(conta.Id, cancellationToken);
        var config = await Config(cancellationToken);
        string? token = null;

        Add(items, "MetaConnected", conta is not null && !string.IsNullOrWhiteSpace(conta.AccessTokenProtegido) ? "OK" : "ERROR", conta is null ? "Meta Ads nao conectado." : "Meta Ads conectado.");
        if (conta is not null && conta.AccessTokenExpiraEm is not null && conta.AccessTokenExpiraEm <= DateTime.UtcNow)
        {
            Add(items, "MetaTokenValid", "ERROR", "Token Meta expirado. Reconecte Meta Ads.");
        }
        else if (conta is not null)
        {
            Add(items, "MetaTokenValid", "OK", "Token Meta disponivel para validacoes de leitura.");
        }

        if (!string.IsNullOrWhiteSpace(conta?.AccessTokenProtegido))
        {
            token = protector.Unprotect(conta.AccessTokenProtegido);
        }

        var assets = await AssetsAsync(config, token, selecao, items, cancellationToken);
        var landing = await publicUrlBuilder.BuildAsync(campanha.Slug, campanha.UrlPublica, cancellationToken);
        ValidateLanding(campanha, landing, items);

        var category = NormalizeSpecialCategory(request.SpecialAdCategory);
        Add(items, "SpecialAdCategory", category.Valid ? "OK" : "ERROR", category.Message);

        var preparacao = await preparacaoRepository.ObterPorCampanhaAsync(campanha.Id, cancellationToken);
        var targeting = await BuildTargetingAsync(config, token, campanha, request, preparacao, items, cancellationToken);
        var budget = campanha.OrcamentoDiario;
        long? budgetMinorUnits = null;
        if (budget <= 0)
        {
            Add(items, "BudgetValid", "ERROR", "Orcamento diario deve ser maior que zero.");
        }
        else if (string.IsNullOrWhiteSpace(assets.Currency))
        {
            Add(items, "BudgetValid", "OK", "Orcamento diario maior que zero.");
            Add(items, "CurrencyValid", "ERROR", "Moeda da Ad Account nao identificada. Recarregue os ativos Meta antes de publicar.");
        }
        else
        {
            budgetMinorUnits = MetaAdsMoney.ToMinorUnits(budget, assets.Currency);
            Add(items, "BudgetValid", "OK", "Orcamento diario maior que zero.");
            Add(items, "CurrencyValid", "OK", $"Moeda validada pela Ad Account: {assets.Currency}.");
        }

        var media = await MediaAsync(campanha.Id, conta, selecao, items, cancellationToken);
        var copy = BuildCopy(campanha, landing.Url ?? campanha.UrlPublica ?? string.Empty, items, media) with
        {
            PageId = selecao?.PageId,
            InstagramAccountId = selecao?.InstagramAccountId
        };
        Add(items, "PixelRequired", "OK", "Pixel nao e obrigatorio para o objetivo inicial OUTCOME_TRAFFIC com otimizacao LINK_CLICKS.");
        Add(items, "PixelValid", assets.PixelValidStatus, assets.PixelValidMessage);
        await AddPermissionPreflightAsync(config, token, items, cancellationToken);

        var ready = items.All(x => !string.Equals(x.Status, "ERROR", StringComparison.OrdinalIgnoreCase));
        Add(items, "ReadyToPublish", ready ? "OK" : "ERROR", ready ? "Preview pronto para a Etapa 4B.2 criar recursos pausados." : "Ainda existem bloqueadores antes da publicacao real.");
        return new MetaAdsPreviewResponse(
            campanha.Id,
            new MetaAdsPreviewAssets(selecao?.BusinessId, selecao?.BusinessNome, selecao?.AdAccountId, selecao?.AdAccountNome, selecao?.PageId, selecao?.PageNome, selecao?.InstagramAccountId, selecao?.InstagramNome, selecao?.PixelId, selecao?.PixelNome),
            new MetaAdsCampaignPreview(campanha.Nome, InitialObjective, PlannedStatus, category.Value, category.Value == "NONE" ? [] : [category.Value]),
            new MetaAdsAdSetPreview(
                $"{campanha.Nome} - conjunto",
                InitialObjective,
                budget,
                budgetMinorUnits,
                assets.Currency,
                "IMPRESSIONS",
                InitialOptimizationGoal,
                "LOWEST_COST_WITHOUT_CAP",
                targeting,
                null,
                null,
                selecao?.PixelId),
            copy,
            new MetaAdsAdPreview($"{campanha.Nome} - anuncio", PlannedStatus),
            new MetaAdsPreflight(ready, items));
    }

    private async Task<(string? Currency, string PixelValidStatus, string PixelValidMessage)> AssetsAsync(MetaAdsConfiguration config, string? token, MetaAdsAtivoSelecionado? selecao, List<MetaAdsPreflightItem> items, CancellationToken cancellationToken)
    {
        Add(items, "BusinessSelected", string.IsNullOrWhiteSpace(selecao?.BusinessId) ? "ERROR" : "OK", string.IsNullOrWhiteSpace(selecao?.BusinessId) ? "Selecione um Business Meta em Configuracoes." : "Business selecionado.");
        Add(items, "AdAccountSelected", string.IsNullOrWhiteSpace(selecao?.AdAccountId) ? "ERROR" : "OK", string.IsNullOrWhiteSpace(selecao?.AdAccountId) ? "Selecione uma Ad Account Meta em Configuracoes." : "Ad Account selecionada.");
        Add(items, "PageSelected", string.IsNullOrWhiteSpace(selecao?.PageId) ? "ERROR" : "OK", string.IsNullOrWhiteSpace(selecao?.PageId) ? "Selecione uma Facebook Page em Configuracoes." : "Facebook Page selecionada.");

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(selecao?.BusinessId) || string.IsNullOrWhiteSpace(selecao.AdAccountId))
        {
            return (null, "WARNING", "Pixel/Dataset opcional neste fluxo.");
        }

        try
        {
            var business = (await graphClient.ListBusinessesAsync(config, token, cancellationToken)).FirstOrDefault(x => Same(x.Id, selecao.BusinessId));
            Add(items, "BusinessAccessible", business is null ? "ERROR" : "OK", business is null ? "Business selecionado nao esta acessivel com a conexao Meta atual." : "Business selecionado validado no Graph API.");

            var adAccount = (await graphClient.ListAdAccountsAsync(config, token, selecao.BusinessId, cancellationToken))
                .FirstOrDefault(x => Same(x.Id, selecao.AdAccountId) || Same(x.AccountId, selecao.AdAccountId));
            if (adAccount is null)
            {
                Add(items, "AdAccountAccessible", "ERROR", "Ad Account selecionada nao esta acessivel com a conexao Meta atual.");
                return (null, "WARNING", "Pixel/Dataset opcional neste fluxo.");
            }

            Add(items, "AdAccountAccessible", "OK", "Ad Account selecionada validada no Graph API.");
            var accountUsable = IsUsableAdAccount(adAccount.Status);
            Add(items, "AdAccountUsable", accountUsable ? "OK" : "ERROR", accountUsable ? "Ad Account em status utilizavel para anuncios." : $"Ad Account em status nao utilizavel ou desconhecido: {adAccount.Status ?? "nao informado"}.");

            if (!string.IsNullOrWhiteSpace(selecao.PageId))
            {
                var page = await graphClient.GetPageAsync(config, token, selecao.PageId, cancellationToken);
                Add(items, "PageAccessible", page is null ? "ERROR" : "OK", page is null ? "Facebook Page selecionada nao esta acessivel." : "Facebook Page selecionada validada.");
                if (!string.IsNullOrWhiteSpace(selecao.InstagramAccountId))
                {
                    var instagramValid = Same(page?.Instagram?.Id, selecao.InstagramAccountId);
                    Add(items, "InstagramValid", instagramValid ? "OK" : "ERROR", instagramValid ? "Instagram ainda corresponde a Page selecionada." : "Instagram selecionado nao corresponde mais a Page.");
                }
                else
                {
                    Add(items, "InstagramValid", "WARNING", "Instagram Professional nao selecionado; o preview usara apenas a Page.");
                }
            }

            if (string.IsNullOrWhiteSpace(selecao.PixelId))
            {
                return (adAccount.Moeda, "WARNING", "Nenhum Pixel/Dataset selecionado; conversao Meta ficara para etapa posterior.");
            }

            var pixel = (await graphClient.ListPixelsAsync(config, token, adAccount.Id, cancellationToken)).FirstOrDefault(x => Same(x.Id, selecao.PixelId));
            return (adAccount.Moeda, pixel is null ? "ERROR" : "OK", pixel is null ? "Pixel/Dataset selecionado nao pertence mais a Ad Account." : "Pixel/Dataset validado na Ad Account.");
        }
        catch (MetaAdsGraphApiException ex) when (ex.PermissionRequired)
        {
            Add(items, "AdAccountAccessible", "ERROR", "Permissao Meta insuficiente para validar a Ad Account selecionada. Reconecte autorizando os escopos de leitura.");
        }
        catch (MetaAdsGraphApiException)
        {
            Add(items, "AdAccountAccessible", "ERROR", "Nao foi possivel validar a Ad Account selecionada no Graph API.");
        }

        return (null, "WARNING", "Pixel/Dataset opcional neste fluxo.");
    }

    private async Task AddPermissionPreflightAsync(MetaAdsConfiguration config, string? token, List<MetaAdsPreflightItem> items, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            Add(items, "AdsManagementGranted", "ERROR", "Nao foi possivel verificar ads_management sem token Meta.");
            return;
        }

        try
        {
            var permissions = await graphClient.GetPermissionsAsync(config, token, cancellationToken);
            var hasAdsManagement = permissions.Granted.Contains("ads_management", StringComparer.OrdinalIgnoreCase);
            Add(items, "AdsManagementGranted", hasAdsManagement ? "OK" : "ERROR", hasAdsManagement ? "ads_management concedido para publicacao futura." : "ads_management ainda nao foi concedido. A publicacao real exigira reconexao/autorizacao adicional.");
        }
        catch (MetaAdsGraphApiException)
        {
            Add(items, "AdsManagementGranted", "ERROR", "Nao foi possivel verificar permissoes Meta no Graph API.");
        }
    }

    private static void ValidateLanding(Campanha campanha, CampaignPublicUrlBuildResult landing, List<MetaAdsPreflightItem> items)
    {
        if (!campanha.Publicada)
        {
            Add(items, "LandingPublished", "ERROR", "Landing precisa estar publicada antes da publicacao Meta.");
            Add(items, "DestinationUrlValid", "ERROR", "URL de destino indisponivel.");
            return;
        }

        if (!landing.Valida || string.IsNullOrWhiteSpace(landing.Url) || !Uri.TryCreate(landing.Url, UriKind.Absolute, out var uri))
        {
            Add(items, "LandingPublished", "ERROR", landing.MotivoFalha ?? "URL publica da landing invalida.");
            Add(items, "DestinationUrlValid", "ERROR", landing.MotivoFalha ?? "URL publica da landing invalida.");
            return;
        }

        if (uri.Host.Contains("localhost", StringComparison.OrdinalIgnoreCase) || uri.Host is "127.0.0.1")
        {
            Add(items, "LandingPublished", "ERROR", "URL da landing nao pode apontar para localhost na publicacao Meta.");
            Add(items, "DestinationUrlValid", "ERROR", "URL da landing nao pode apontar para localhost na publicacao Meta.");
            return;
        }

        if (uri.Scheme != Uri.UriSchemeHttps)
        {
            Add(items, "LandingPublished", "ERROR", "URL da landing deve usar HTTPS para publicacao Meta.");
            Add(items, "DestinationUrlValid", "ERROR", "URL da landing deve usar HTTPS para publicacao Meta.");
            return;
        }

        Add(items, "LandingPublished", "OK", "Landing publica valida para preview Meta.");
        Add(items, "DestinationUrlValid", "OK", "URL de destino HTTPS valida.");
    }

    private async Task<MetaAdsTargetingPreview> BuildTargetingAsync(MetaAdsConfiguration config, string? token, Campanha campanha, MetaAdsPreviewRequest request, MetaAdsPreparacaoPublicacao? preparacao, List<MetaAdsPreflightItem> items, CancellationToken cancellationToken)
    {
        var ageMin = request.IdadeMinima ?? preparacao?.AgeMin ?? 25;
        var ageMax = request.IdadeMaxima ?? preparacao?.AgeMax ?? 65;
        if (ageMin < 18 || ageMax < 18 || ageMin > 65 || ageMax > 65 || ageMin > ageMax)
        {
            Add(items, "TargetingValid", "ERROR", "Idade do publico Meta deve estar entre 18 e 65, com minimo menor ou igual ao maximo.");
        }
        else
        {
            Add(items, "TargetingValid", "OK", "Faixa etaria valida.");
        }

        var country = await DefaultCountryAsync(cancellationToken);
        MetaAdsLocationResponse? location = null;
        var selectedKey = request.LocationKey ?? preparacao?.LocationKey;
        if (!string.IsNullOrWhiteSpace(selectedKey) && !string.IsNullOrWhiteSpace(token))
        {
            var query = string.IsNullOrWhiteSpace(campanha.Cidade) ? country : campanha.Cidade;
            var options = await graphClient.SearchTargetingLocationsAsync(config, token, query, country, 25, cancellationToken);
            location = options.FirstOrDefault(x => Same(x.Key, selectedKey));
        }
        else if (!string.IsNullOrWhiteSpace(preparacao?.LocationKey))
        {
            location = new MetaAdsLocationResponse(
                preparacao.LocationKey,
                preparacao.LocationName ?? "Localizacao Meta",
                preparacao.LocationType ?? "unknown",
                preparacao.CountryCode,
                preparacao.CountryName,
                preparacao.Region,
                preparacao.RegionId,
                string.Equals(preparacao.LocationType, "region", StringComparison.OrdinalIgnoreCase),
                string.Equals(preparacao.LocationType, "city", StringComparison.OrdinalIgnoreCase));
        }

        Add(items, "MetaLocationsResolved", location is null ? "ERROR" : "OK", location is null ? "Selecione uma localizacao Meta valida pelo autocomplete antes da publicacao." : $"Localizacao Meta resolvida: {location.Name} ({location.Type}).");
        return new MetaAdsTargetingPreview([country], location, campanha.Estado, campanha.Cidade, ageMin, ageMax);
    }

    private static MetaAdsCreativePreview BuildCopy(Campanha campanha, string destinationUrl, List<MetaAdsPreflightItem> items, MetaAdsImagem? media)
    {
        var headlines = Deserialize<string>(campanha.TitulosAnunciosJson);
        var descriptions = Deserialize<string>(campanha.DescricoesAnunciosJson);
        var benefits = Deserialize<string>(campanha.BeneficiosJson);
        var primaryText = First(campanha.SubtituloLandingPage, campanha.Objetivo, benefits.FirstOrDefault());
        var headline = First(headlines.FirstOrDefault(), campanha.TituloLandingPage, campanha.Nome);
        var description = First(descriptions.FirstOrDefault(), campanha.Objetivo, campanha.SubtituloLandingPage);
        var copyValid = !string.IsNullOrWhiteSpace(primaryText) && !string.IsNullOrWhiteSpace(headline) && !string.IsNullOrWhiteSpace(description) && !string.IsNullOrWhiteSpace(destinationUrl);
        Add(items, "CreativeContentValid", copyValid ? "OK" : "ERROR", copyValid ? "Texto minimo do creative Meta disponivel." : "Creative Meta incompleto: texto, headline, descricao e URL sao obrigatorios.");
        return new MetaAdsCreativePreview(null, null, primaryText, headline, description, destinationUrl, "LEARN_MORE", null, media?.NomeArquivo, media?.MetaImageHash, !string.IsNullOrWhiteSpace(media?.MetaImageHash));
    }

    private async Task<MetaAdsImagem?> MediaAsync(Guid campanhaId, MetaAdsConta? conta, MetaAdsAtivoSelecionado? selecao, List<MetaAdsPreflightItem> items, CancellationToken cancellationToken)
    {
        if (conta is null || string.IsNullOrWhiteSpace(selecao?.AdAccountId))
        {
            Add(items, "MediaSelected", "ERROR", "Selecione Ad Account antes de escolher midia.");
            Add(items, "MediaValid", "ERROR", "Midia Meta ausente.");
            Add(items, "MediaUploaded", "ERROR", "Imagem ainda nao enviada para a Ad Account.");
            Add(items, "MetaImageHashAvailable", "ERROR", "Meta image_hash ausente.");
            return null;
        }

        var imagem = await imagemRepository.ObterPorCampanhaAsync(campanhaId, selecao.AdAccountId, cancellationToken);
        Add(items, "MediaSelected", imagem is null ? "ERROR" : "OK", imagem is null ? "Selecione uma imagem para publicar no Meta Ads." : $"Imagem selecionada: {imagem.NomeArquivo}.");
        Add(items, "MediaValid", imagem is null ? "ERROR" : "OK", imagem is null ? "Imagem Meta ausente." : "Imagem validada pelo backend antes do upload.");
        Add(items, "MediaUploaded", string.IsNullOrWhiteSpace(imagem?.MetaImageHash) ? "ERROR" : "OK", string.IsNullOrWhiteSpace(imagem?.MetaImageHash) ? "Imagem ainda nao enviada para a Ad Account." : "Imagem enviada para a Ad Account.");
        Add(items, "MetaImageHashAvailable", string.IsNullOrWhiteSpace(imagem?.MetaImageHash) ? "ERROR" : "OK", string.IsNullOrWhiteSpace(imagem?.MetaImageHash) ? "Meta image_hash ausente." : "Meta image_hash disponivel para o creative futuro.");
        return imagem;
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

    private async Task<string> DefaultCountryAsync(CancellationToken cancellationToken)
    {
        return (await Value("DefaultCountryCode", cancellationToken))?.Trim().ToUpperInvariant() ?? "BR";
    }

    private async Task<string?> Value(string key, CancellationToken cancellationToken)
    {
        return (await resolver.ResolveAsync(CategoriaConfiguracao.MetaAds, key, cancellationToken)).Value;
    }

    private static (bool Valid, string Value, string Message) NormalizeSpecialCategory(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "NONE" : value.Trim().ToUpperInvariant();
        return SupportedSpecialCategories.Contains(normalized)
            ? (true, normalized, normalized == "NONE" ? "Categoria especial explicitamente definida como nenhuma." : $"Categoria especial definida: {normalized}.")
            : (false, normalized, "Categoria especial Meta invalida.");
    }

    private static IReadOnlyList<T> Deserialize<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<IReadOnlyList<T>>(json, JsonOptions) ?? [];
    }

    private static string First(params string?[] values)
    {
        return values.Select(x => x?.Trim()).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;
    }

    private static void Add(List<MetaAdsPreflightItem> items, string code, string status, string message)
    {
        items.Add(new MetaAdsPreflightItem(code, status, message));
    }

    private static bool Same(string? left, string? right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUsableAdAccount(string? status)
    {
        return status is "1" or "ACTIVE";
    }
}
