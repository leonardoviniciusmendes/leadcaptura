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

    private sealed record MetaAdsDiagnosticsContext(MetaAdsConfiguration Config, string AccessToken, string AdAccountId);
}
