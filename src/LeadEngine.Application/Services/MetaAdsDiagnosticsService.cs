using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Services;

public sealed class MetaAdsDiagnosticsService(
    IConfigurationResolver resolver,
    IMetaAdsGraphClient graphClient) : IMetaAdsDiagnosticsService
{
    public async Task<MetaAdAccountDto> GetAdAccountAsync(CancellationToken cancellationToken)
    {
        var context = await ResolveContextAsync(cancellationToken);
        return await graphClient.GetAdAccountAsync(context.Config, context.AccessToken, context.AdAccountId, cancellationToken);
    }

    public async Task<IReadOnlyList<MetaCampaignDto>> GetCampaignsAsync(CancellationToken cancellationToken)
    {
        var context = await ResolveContextAsync(cancellationToken);
        return await graphClient.GetCampaignsAsync(context.Config, context.AccessToken, context.AdAccountId, cancellationToken);
    }

    public async Task<IReadOnlyList<MetaAdSetDto>> GetAdSetsAsync(CancellationToken cancellationToken)
    {
        var context = await ResolveContextAsync(cancellationToken);
        return await graphClient.GetAdSetsAsync(context.Config, context.AccessToken, context.AdAccountId, cancellationToken);
    }

    public async Task<IReadOnlyList<MetaAdDto>> GetAdsAsync(CancellationToken cancellationToken)
    {
        var context = await ResolveContextAsync(cancellationToken);
        return await graphClient.GetAdsAsync(context.Config, context.AccessToken, context.AdAccountId, cancellationToken);
    }

    public async Task<IReadOnlyList<MetaCreativeDto>> GetCreativesAsync(CancellationToken cancellationToken)
    {
        var context = await ResolveContextAsync(cancellationToken);
        return await graphClient.GetAdCreativesAsync(context.Config, context.AccessToken, context.AdAccountId, cancellationToken);
    }

    public async Task<CreateMetaCampaignResponse> CreateCampaignAsync(CreateMetaCampaignRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Nome da campanha Meta e obrigatorio.");
        }

        var context = await ResolveContextAsync(cancellationToken);
        var result = await graphClient.CreateCampaignAsync(
            context.Config,
            context.AccessToken,
            context.AdAccountId,
            new MetaAdsCampaignCreatePayload(
                request.Name.Trim(),
                string.IsNullOrWhiteSpace(request.Objective) ? MetaAdsConstants.ObjectiveOutcomeLeads : request.Objective.Trim(),
                request.SpecialAdCategories?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray()
                    ?? MetaAdsConstants.NoSpecialAdCategories,
                MetaAdsConstants.StatusPaused),
            cancellationToken);

        return new CreateMetaCampaignResponse(result.Id);
    }

    public async Task<DeleteMetaCampaignResponse> DeleteCampaignAsync(string campaignId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(campaignId))
        {
            throw new ArgumentException("ID da campanha Meta e obrigatorio.");
        }

        var context = await ResolveContextAsync(cancellationToken);
        await graphClient.DeleteCampaignAsync(context.Config, context.AccessToken, campaignId.Trim(), cancellationToken);
        return new DeleteMetaCampaignResponse(true);
    }

    public async Task<CreateMetaAdSetResponse> CreateAdSetAsync(CreateMetaAdSetRequest request, CancellationToken cancellationToken)
    {
        ValidateAdSetRequest(request);

        var context = await ResolveContextAsync(cancellationToken);
        var targeting = request.Targeting ?? throw new ArgumentException("Targeting do Ad Set e obrigatorio.");
        var result = await graphClient.CreateAdSetAsync(
            context.Config,
            context.AccessToken,
            context.AdAccountId,
            new MetaAdsAdSetCreatePayload(
                request.Name.Trim(),
                request.CampaignId.Trim(),
                string.IsNullOrWhiteSpace(request.OptimizationGoal) ? MetaAdsConstants.OptimizationGoalLeadGeneration : request.OptimizationGoal.Trim(),
                string.IsNullOrWhiteSpace(request.BillingEvent) ? MetaAdsConstants.BillingEventImpressions : request.BillingEvent.Trim(),
                request.DailyBudget,
                MetaAdsConstants.BidStrategyLowestCostWithoutCap,
                new MetaAdsTargetingCreatePayload(
                    CleanStrings(targeting.Countries),
                    CleanLocationKeys(targeting.Regions),
                    CleanLocationKeys(targeting.Cities),
                    targeting.AgeMin,
                    targeting.AgeMax,
                    targeting.Genders?.ToArray(),
                    0),
                MetaAdsConstants.StatusPaused,
                request.StartTime,
                request.EndTime),
            cancellationToken);

        return new CreateMetaAdSetResponse(result.Id);
    }

    public async Task<DeleteMetaAdSetResponse> DeleteAdSetAsync(string adSetId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(adSetId))
        {
            throw new ArgumentException("ID do Ad Set Meta e obrigatorio.");
        }

        var context = await ResolveContextAsync(cancellationToken);
        await graphClient.DeleteAdSetAsync(context.Config, context.AccessToken, adSetId.Trim(), cancellationToken);
        return new DeleteMetaAdSetResponse(true);
    }

    public async Task<CreateMetaCreativeResponse> CreateCreativeAsync(CreateMetaCreativeRequest request, CancellationToken cancellationToken)
    {
        ValidateCreativeRequest(request);

        var context = await ResolveContextAsync(cancellationToken);
        var result = await graphClient.CreateDiagnosticAdCreativeAsync(
            context.Config,
            context.AccessToken,
            context.AdAccountId,
            new MetaAdsDiagnosticCreativeCreatePayload(
                request.Name.Trim(),
                request.PageId.Trim(),
                request.ImageHash.Trim(),
                request.LinkUrl.Trim(),
                request.Message.Trim(),
                request.Headline.Trim(),
                string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                string.IsNullOrWhiteSpace(request.CallToActionType) ? null : request.CallToActionType.Trim().ToUpperInvariant()),
            cancellationToken);

        return new CreateMetaCreativeResponse(result.Id);
    }

    public async Task<DeleteMetaCreativeResponse> DeleteCreativeAsync(string creativeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(creativeId))
        {
            throw new ArgumentException("ID do Creative Meta e obrigatorio.");
        }

        var context = await ResolveContextAsync(cancellationToken);
        await graphClient.DeleteAdCreativeAsync(context.Config, context.AccessToken, creativeId.Trim(), cancellationToken);
        return new DeleteMetaCreativeResponse(true);
    }

    public async Task<CreateMetaAdResponse> CreateAdAsync(CreateMetaAdRequest request, CancellationToken cancellationToken)
    {
        ValidateAdRequest(request);

        var context = await ResolveContextAsync(cancellationToken);
        var result = await graphClient.CreateAdAsync(
            context.Config,
            context.AccessToken,
            context.AdAccountId,
            new MetaAdsAdCreatePayload(
                request.Name.Trim(),
                request.AdSetId.Trim(),
                request.CreativeId.Trim(),
                MetaAdsConstants.StatusPaused),
            cancellationToken);

        return new CreateMetaAdResponse(result.Id);
    }

    public async Task<DeleteMetaAdResponse> DeleteAdAsync(string adId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(adId))
        {
            throw new ArgumentException("ID do Ad Meta e obrigatorio.");
        }

        var context = await ResolveContextAsync(cancellationToken);
        await graphClient.DeleteAdAsync(context.Config, context.AccessToken, adId.Trim(), cancellationToken);
        return new DeleteMetaAdResponse(true);
    }

    private async Task<MetaAdsDiagnosticsContext> ResolveContextAsync(CancellationToken cancellationToken)
    {
        var accessToken = await RequiredAsync("AccessToken", "Configure MetaAds:AccessToken por variavel de ambiente ou segredo protegido.", cancellationToken);
        var adAccountId = await RequiredAsync("AdAccountId", "Configure MetaAds:AdAccountId com o identificador da conta de anuncios.", cancellationToken);
        var graphBaseUrl = await RequiredAsync("GraphApiBaseUrl", "Configure MetaAds:GraphApiBaseUrl.", cancellationToken);
        var graphVersion = await RequiredAsync("GraphApiVersion", "Configure MetaAds:GraphApiVersion.", cancellationToken);

        var config = new MetaAdsConfiguration(
            AppId: string.Empty,
            AppSecret: string.Empty,
            RedirectUri: string.Empty,
            AuthEndpoint: string.Empty,
            TokenEndpoint: string.Empty,
            UserInfoEndpoint: string.Empty,
            GraphApiBaseUrl: graphBaseUrl,
            GraphApiVersion: graphVersion,
            Scopes: string.Empty);

        return new MetaAdsDiagnosticsContext(config, accessToken, adAccountId);
    }

    private async Task<string> RequiredAsync(string key, string message, CancellationToken cancellationToken)
    {
        var value = (await resolver.ResolveAsync(CategoriaConfiguracao.MetaAds, key, cancellationToken)).Value;
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(message);
        }

        return value.Trim();
    }

    private static void ValidateAdSetRequest(CreateMetaAdSetRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CampaignId))
        {
            throw new ArgumentException("ID da campanha Meta e obrigatorio para criar Ad Set.");
        }
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Nome do Ad Set Meta e obrigatorio.");
        }
        if (request.DailyBudget <= 0)
        {
            throw new ArgumentException("Orcamento diario do Ad Set deve ser informado em unidades minimas da moeda e ser maior que zero.");
        }
        if (request.Targeting is null)
        {
            throw new ArgumentException("Targeting do Ad Set e obrigatorio.");
        }
        if (CleanStrings(request.Targeting.Countries).Count == 0
            && CleanLocationKeys(request.Targeting.Regions).Count == 0
            && CleanLocationKeys(request.Targeting.Cities).Count == 0)
        {
            throw new ArgumentException("Informe ao menos uma localizacao valida no targeting Meta.");
        }
        if (request.Targeting.AgeMin is not null && request.Targeting.AgeMin is < 13 or > 65)
        {
            throw new ArgumentException("Idade minima Meta deve estar entre 13 e 65.");
        }
        if (request.Targeting.AgeMax is not null && request.Targeting.AgeMax is < 13 or > 65)
        {
            throw new ArgumentException("Idade maxima Meta deve estar entre 13 e 65.");
        }
        if (request.Targeting.AgeMin is not null && request.Targeting.AgeMax is not null && request.Targeting.AgeMax < request.Targeting.AgeMin)
        {
            throw new ArgumentException("Idade maxima Meta deve ser maior ou igual a idade minima.");
        }
        if (request.Targeting.Genders?.Any(x => x is not 1 and not 2) == true)
        {
            throw new ArgumentException("Genders Meta deve conter apenas 1 ou 2 quando informado.");
        }
        if (request.StartTime is not null && request.EndTime is not null && request.EndTime <= request.StartTime)
        {
            throw new ArgumentException("EndTime do Ad Set deve ser maior que StartTime.");
        }
    }

    private static void ValidateCreativeRequest(CreateMetaCreativeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Nome do Creative Meta e obrigatorio.");
        }
        if (string.IsNullOrWhiteSpace(request.PageId))
        {
            throw new ArgumentException("PageId do Creative Meta e obrigatorio.");
        }
        if (string.IsNullOrWhiteSpace(request.ImageHash))
        {
            throw new ArgumentException("ImageHash do Creative Meta e obrigatorio.");
        }
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("Message do Creative Meta e obrigatorio.");
        }
        if (string.IsNullOrWhiteSpace(request.LinkUrl)
            || !Uri.TryCreate(request.LinkUrl.Trim(), UriKind.Absolute, out var uri)
            || (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("LinkUrl do Creative Meta deve ser uma URL absoluta valida.");
        }
        if (string.IsNullOrWhiteSpace(request.Headline))
        {
            throw new ArgumentException("Headline do Creative Meta e obrigatorio.");
        }
        if (!string.IsNullOrWhiteSpace(request.CallToActionType)
            && !string.Equals(request.CallToActionType.Trim(), "LEARN_MORE", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("CallToActionType Meta suportado nesta fase: LEARN_MORE.");
        }
    }

    private static void ValidateAdRequest(CreateMetaAdRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Nome do Ad Meta e obrigatorio.");
        }
        if (string.IsNullOrWhiteSpace(request.AdSetId))
        {
            throw new ArgumentException("AdSetId do Ad Meta e obrigatorio.");
        }
        if (string.IsNullOrWhiteSpace(request.CreativeId))
        {
            throw new ArgumentException("CreativeId do Ad Meta e obrigatorio.");
        }
    }

    private static IReadOnlyList<string> CleanStrings(IReadOnlyList<string>? values)
    {
        return values?.Select(x => x?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
            ?? [];
    }

    private static IReadOnlyList<MetaAdsTargetingLocationPayload> CleanLocationKeys(IReadOnlyList<MetaLocationKeyRequest>? values)
    {
        return values?.Select(x => x.Key?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => new MetaAdsTargetingLocationPayload(x!))
                .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .ToArray()
            ?? [];
    }

    private sealed record MetaAdsDiagnosticsContext(MetaAdsConfiguration Config, string AccessToken, string AdAccountId);
}
