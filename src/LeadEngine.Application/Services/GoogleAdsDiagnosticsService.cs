using System.Text.Json;
using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Services;

public sealed class GoogleAdsDiagnosticsService(
    IGoogleAdsContaRepository contaRepository,
    IGoogleAdsTokenService tokenService,
    IGoogleAdsDiagnosticsQueryClient queryClient,
    IGoogleAdsMutationClient mutationClient,
    IConfigurationResolver resolver) : IGoogleAdsDiagnosticsService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<GoogleAdsDiagnosticAccountResponse> GetAccountAsync(CancellationToken cancellationToken)
    {
        var conta = await RequiredAccountAsync(cancellationToken);
        return new GoogleAdsDiagnosticAccountResponse(
            conta.Id,
            conta.CustomerId,
            GoogleAdsCustomerId.Mask(conta.CustomerId),
            conta.Nome,
            conta.Ativa,
            conta.Padrao);
    }

    public async Task<IReadOnlyList<GoogleAdsDiagnosticCampaignDto>> GetCampaignsAsync(CancellationToken cancellationToken)
    {
        var context = await ResolveContextAsync(cancellationToken);
        return await queryClient.GetCampaignsAsync(context.Conta.CustomerId, context.AccessToken, context.DeveloperToken, cancellationToken);
    }

    public async Task<IReadOnlyList<GoogleAdsDiagnosticAdGroupDto>> GetAdGroupsAsync(CancellationToken cancellationToken)
    {
        var context = await ResolveContextAsync(cancellationToken);
        return await queryClient.GetAdGroupsAsync(context.Conta.CustomerId, context.AccessToken, context.DeveloperToken, cancellationToken);
    }

    public async Task<IReadOnlyList<GoogleAdsDiagnosticKeywordDto>> GetKeywordsAsync(CancellationToken cancellationToken)
    {
        var context = await ResolveContextAsync(cancellationToken);
        return await queryClient.GetKeywordsAsync(context.Conta.CustomerId, context.AccessToken, context.DeveloperToken, cancellationToken);
    }

    public async Task<IReadOnlyList<GoogleAdsDiagnosticResponsiveSearchAdDto>> GetResponsiveSearchAdsAsync(CancellationToken cancellationToken)
    {
        var context = await ResolveContextAsync(cancellationToken);
        return await queryClient.GetResponsiveSearchAdsAsync(context.Conta.CustomerId, context.AccessToken, context.DeveloperToken, cancellationToken);
    }

    public async Task<CreateGoogleAdsDiagnosticCampaignResponse> CreateCampaignAsync(CreateGoogleAdsDiagnosticCampaignRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Nome da campanha Google Ads e obrigatorio.");
        }
        if (request.DailyBudgetMicros <= 0)
        {
            throw new ArgumentException("DailyBudgetMicros deve ser maior que zero.");
        }

        var context = await ResolveContextAsync(cancellationToken);
        var customerId = GoogleAdsCustomerId.Normalize(context.Conta.CustomerId);
        var budgetResource = $"customers/{customerId}/campaignBudgets/-1";
        var campaignResource = $"customers/{customerId}/campaigns/-2";
        var name = request.Name.Trim();
        var plan = new GoogleAdsOperationPlan(
            "diagnostic",
            1,
            customerId,
            string.Empty,
            string.Empty,
            [
                Op("Budget", $"{name} - Budget", "CampaignBudgetOperation", budgetResource, new
                {
                    resourceName = budgetResource,
                    name = $"{name} - Budget",
                    amountMicros = request.DailyBudgetMicros
                }),
                Op("Campaign", name, "CampaignOperation", campaignResource, new
                {
                    resourceName = campaignResource,
                    name,
                    budgetResource
                })
            ],
            []);

        var result = await mutationClient.MutateAsync(context.Conta.CustomerId, context.AccessToken, context.DeveloperToken, plan, validateOnly: false, cancellationToken);
        if (!result.Success)
        {
            throw new GoogleAdsDiagnosticException(new GoogleAdsDiagnosticResponse(
                false,
                result.Errors.FirstOrDefault()?.Codigo ?? "google_ads_diagnostic_campaign_create_failed",
                result.Errors.FirstOrDefault()?.Mensagem ?? "Google Ads rejeitou a criacao diagnostica da Campaign.",
                result.RequestId,
                result.Errors));
        }

        return new CreateGoogleAdsDiagnosticCampaignResponse(result.RequestId, result.Resources);
    }

    public async Task<CreateGoogleAdsDiagnosticAdGroupResponse> CreateAdGroupAsync(CreateGoogleAdsDiagnosticAdGroupRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CampaignResourceName))
        {
            throw new ArgumentException("CampaignResourceName e obrigatorio.");
        }
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Nome do Ad Group Google Ads e obrigatorio.");
        }

        var context = await ResolveContextAsync(cancellationToken);
        var customerId = GoogleAdsCustomerId.Normalize(context.Conta.CustomerId);
        var adGroupResource = $"customers/{customerId}/adGroups/-1";
        var name = request.Name.Trim();
        var plan = new GoogleAdsOperationPlan(
            "diagnostic",
            1,
            customerId,
            string.Empty,
            string.Empty,
            [
                Op("AdGroup", name, "AdGroupOperation", adGroupResource, new
                {
                    resourceName = adGroupResource,
                    campaignResource = request.CampaignResourceName.Trim(),
                    name
                })
            ],
            []);

        var result = await mutationClient.MutateAsync(context.Conta.CustomerId, context.AccessToken, context.DeveloperToken, plan, validateOnly: false, cancellationToken);
        if (!result.Success)
        {
            throw new GoogleAdsDiagnosticException(new GoogleAdsDiagnosticResponse(
                false,
                result.Errors.FirstOrDefault()?.Codigo ?? "google_ads_diagnostic_ad_group_create_failed",
                result.Errors.FirstOrDefault()?.Mensagem ?? "Google Ads rejeitou a criacao diagnostica do Ad Group.",
                result.RequestId,
                result.Errors));
        }

        return new CreateGoogleAdsDiagnosticAdGroupResponse(result.RequestId, result.Resources);
    }

    public async Task<CreateGoogleAdsDiagnosticKeywordsResponse> CreateKeywordsAsync(CreateGoogleAdsDiagnosticKeywordsRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AdGroupResourceName))
        {
            throw new ArgumentException("AdGroupResourceName e obrigatorio.");
        }
        if (request.Keywords is null || request.Keywords.Count == 0)
        {
            throw new ArgumentException("Informe ao menos uma keyword.");
        }

        var context = await ResolveContextAsync(cancellationToken);
        var customerId = GoogleAdsCustomerId.Normalize(context.Conta.CustomerId);
        var adGroupResource = request.AdGroupResourceName.Trim();
        var operations = request.Keywords.Select(keyword =>
        {
            var text = keyword.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Texto da keyword e obrigatorio.");
            }

            var matchType = NormalizeKeywordMatchType(keyword.MatchType);
            return Op("Keyword", text, "AdGroupCriterionOperation", null, new
            {
                adGroupResource,
                text,
                matchType
            });
        }).ToArray();

        var plan = new GoogleAdsOperationPlan(
            "diagnostic",
            1,
            customerId,
            string.Empty,
            string.Empty,
            operations,
            []);

        var result = await mutationClient.MutateAsync(context.Conta.CustomerId, context.AccessToken, context.DeveloperToken, plan, validateOnly: false, cancellationToken);
        if (!result.Success)
        {
            throw new GoogleAdsDiagnosticException(new GoogleAdsDiagnosticResponse(
                false,
                result.Errors.FirstOrDefault()?.Codigo ?? "google_ads_diagnostic_keywords_create_failed",
                result.Errors.FirstOrDefault()?.Mensagem ?? "Google Ads rejeitou a criacao diagnostica das Keywords.",
                result.RequestId,
                result.Errors));
        }

        return new CreateGoogleAdsDiagnosticKeywordsResponse(result.RequestId, result.Resources);
    }

    public async Task<CreateGoogleAdsDiagnosticResponsiveSearchAdResponse> CreateResponsiveSearchAdAsync(CreateGoogleAdsDiagnosticResponsiveSearchAdRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AdGroupResourceName))
        {
            throw new ArgumentException("AdGroupResourceName e obrigatorio.");
        }

        var finalUrl = NormalizeFinalHttpsUrl(request.FinalUrl);
        var headlines = NormalizeTextItems(request.Headlines, "Headlines", 3, 15, 30);
        var descriptions = NormalizeTextItems(request.Descriptions, "Descriptions", 2, 4, 90);
        var context = await ResolveContextAsync(cancellationToken);
        var customerId = GoogleAdsCustomerId.Normalize(context.Conta.CustomerId);
        var adGroupResource = request.AdGroupResourceName.Trim();
        var plan = new GoogleAdsOperationPlan(
            "diagnostic",
            1,
            customerId,
            string.Empty,
            string.Empty,
            [
                Op("ResponsiveSearchAd", "Diagnostic RSA", "AdGroupAdOperation", null, new
                {
                    adGroupResource,
                    finalUrls = new[] { finalUrl },
                    headlines,
                    descriptions
                })
            ],
            []);

        var result = await mutationClient.MutateAsync(context.Conta.CustomerId, context.AccessToken, context.DeveloperToken, plan, validateOnly: false, cancellationToken);
        if (!result.Success)
        {
            throw new GoogleAdsDiagnosticException(new GoogleAdsDiagnosticResponse(
                false,
                result.Errors.FirstOrDefault()?.Codigo ?? "google_ads_diagnostic_responsive_search_ad_create_failed",
                result.Errors.FirstOrDefault()?.Mensagem ?? "Google Ads rejeitou a criacao diagnostica do Responsive Search Ad.",
                result.RequestId,
                result.Errors));
        }

        return new CreateGoogleAdsDiagnosticResponsiveSearchAdResponse(result.RequestId, result.Resources);
    }

    private async Task<GoogleAdsDiagnosticsContext> ResolveContextAsync(CancellationToken cancellationToken)
    {
        var conta = await RequiredAccountAsync(cancellationToken);
        var accessToken = await tokenService.ObterAccessTokenValidoAsync(conta, cancellationToken);
        var developerToken = (await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "DeveloperToken", cancellationToken)).Value;
        if (string.IsNullOrWhiteSpace(developerToken))
        {
            throw new ArgumentException("DeveloperToken Google Ads nao configurado.");
        }

        return new GoogleAdsDiagnosticsContext(conta, accessToken, developerToken.Trim());
    }

    private async Task<GoogleAdsConta> RequiredAccountAsync(CancellationToken cancellationToken)
    {
        var conta = await contaRepository.ObterPadraoAsync(cancellationToken);
        if (conta is null || !conta.Ativa || string.IsNullOrWhiteSpace(conta.CustomerId))
        {
            throw new ArgumentException("Conta Google Ads padrao nao configurada.");
        }

        return conta;
    }

    private static string NormalizeKeywordMatchType(string? matchType)
    {
        var normalized = matchType?.Trim().ToUpperInvariant();
        return normalized switch
        {
            "EXACT" => "EXACT",
            "PHRASE" => "PHRASE",
            "BROAD" => throw new ArgumentException("Broad match nao e permitido nesta fase diagnostica."),
            _ => throw new ArgumentException("MatchType deve ser EXACT ou PHRASE.")
        };
    }

    private static string NormalizeFinalHttpsUrl(string? value)
    {
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("FinalUrl deve ser uma URL HTTPS absoluta.");
        }

        return uri.ToString();
    }

    private static IReadOnlyList<string> NormalizeTextItems(IReadOnlyList<string>? values, string field, int min, int max, int maxLength)
    {
        if (values is null || values.Count == 0)
        {
            throw new ArgumentException($"{field} nao pode estar vazio.");
        }
        if (values.Count < min || values.Count > max)
        {
            throw new ArgumentException($"{field} deve ter entre {min} e {max} itens.");
        }

        var normalized = values.Select(x => x?.Trim() ?? string.Empty).ToArray();
        if (normalized.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException($"{field} nao pode conter texto vazio.");
        }
        if (normalized.Any(x => x.Length > maxLength))
        {
            throw new ArgumentException($"{field} deve ter itens com no maximo {maxLength} caracteres.");
        }

        return normalized;
    }

    private static GoogleAdsOperationItem Op(string type, string name, string operation, string? temporaryResourceName, object payload)
    {
        return new GoogleAdsOperationItem(type, name, operation, JsonSerializer.Serialize(payload, JsonOptions), temporaryResourceName);
    }

    private sealed record GoogleAdsDiagnosticsContext(GoogleAdsConta Conta, string AccessToken, string DeveloperToken);
}
