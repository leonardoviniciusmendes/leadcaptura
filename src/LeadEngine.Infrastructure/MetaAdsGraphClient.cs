using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using Microsoft.Extensions.Logging;

namespace LeadEngine.Infrastructure;

public sealed class MetaAdsGraphClient(IHttpClientFactory httpClientFactory, ILogger<MetaAdsGraphClient> logger) : IMetaAdsGraphClient
{
    private const int MaxPages = 10;

    public async Task<MetaAdAccountDto> GetAdAccountAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, CancellationToken cancellationToken)
    {
        using var json = await GetJsonAsync(GraphUrl(config, NormalizeAdAccountId(adAccountId), new() { ["fields"] = "id,name,account_status,currency,timezone_name" }), accessToken, cancellationToken);
        return new MetaAdAccountDto(
            S(json.RootElement, "id") ?? string.Empty,
            S(json.RootElement, "name"),
            S(json.RootElement, "account_status"),
            S(json.RootElement, "currency"),
            S(json.RootElement, "timezone_name"));
    }

    public async Task<IReadOnlyList<MetaCampaignDto>> GetCampaignsAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, CancellationToken cancellationToken)
    {
        var rows = await GetPagedDataAsync(GraphUrl(config, $"{NormalizeAdAccountId(adAccountId)}/campaigns", new() { ["fields"] = "id,name,status,effective_status,bid_strategy", ["limit"] = "100" }), accessToken, cancellationToken);
        return rows.Select(x => new MetaCampaignDto(S(x, "id") ?? string.Empty, S(x, "name"), S(x, "status"), S(x, "effective_status"), S(x, "bid_strategy")))
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .ToArray();
    }

    public async Task<IReadOnlyList<MetaAdSetDto>> GetAdSetsAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, CancellationToken cancellationToken)
    {
        var rows = await GetPagedDataAsync(GraphUrl(config, $"{NormalizeAdAccountId(adAccountId)}/adsets", new() { ["fields"] = "id,name,status,effective_status,campaign_id,daily_budget,lifetime_budget", ["limit"] = "100" }), accessToken, cancellationToken);
        return rows.Select(x => new MetaAdSetDto(S(x, "id") ?? string.Empty, S(x, "name"), S(x, "status"), S(x, "effective_status"), S(x, "campaign_id"), S(x, "daily_budget"), S(x, "lifetime_budget")))
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .ToArray();
    }

    public async Task<IReadOnlyList<MetaAdDto>> GetAdsAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, CancellationToken cancellationToken)
    {
        var rows = await GetPagedDataAsync(GraphUrl(config, $"{NormalizeAdAccountId(adAccountId)}/ads", new() { ["fields"] = "id,name,status,effective_status,adset_id,campaign_id", ["limit"] = "100" }), accessToken, cancellationToken);
        return rows.Select(x => new MetaAdDto(S(x, "id") ?? string.Empty, S(x, "name"), S(x, "status"), S(x, "effective_status"), S(x, "adset_id"), S(x, "campaign_id")))
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .ToArray();
    }

    public async Task<IReadOnlyList<MetaCreativeDto>> GetAdCreativesAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, CancellationToken cancellationToken)
    {
        var rows = await GetPagedDataAsync(GraphUrl(config, $"{NormalizeAdAccountId(adAccountId)}/adcreatives", new() { ["fields"] = "id,name,status,object_story_id,object_story_spec", ["limit"] = "100" }), accessToken, cancellationToken);
        return rows.Select(x => new MetaCreativeDto(S(x, "id") ?? string.Empty, S(x, "name"), S(x, "status"), S(x, "object_story_id"), S(x, "object_story_spec")))
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .ToArray();
    }

    public async Task<MetaAdsResourceStatusDto?> GetResourceStatusAsync(MetaAdsConfiguration config, string accessToken, string resourceId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, GraphUrl(config, resourceId, new() { ["fields"] = "id,status,effective_status" }));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await httpClientFactory.CreateClient("metaads").SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            if ((int)response.StatusCode == 404 || response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return null;
            }

            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            throw ParseError(text, response.StatusCode, "resource-status");
        }

        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var id = S(json.RootElement, "id");
        return string.IsNullOrWhiteSpace(id)
            ? null
            : new MetaAdsResourceStatusDto(id, S(json.RootElement, "status"), S(json.RootElement, "effective_status"));
    }

    public async Task<IReadOnlyList<MetaAdsBusinessResponse>> ListBusinessesAsync(MetaAdsConfiguration config, string accessToken, CancellationToken cancellationToken)
    {
        var url = GraphUrl(config, "me/businesses", new() { ["fields"] = "id,name", ["limit"] = "100" });
        var rows = await GetPagedDataAsync(url, accessToken, cancellationToken);
        return rows.Select(x => new MetaAdsBusinessResponse(S(x, "id") ?? string.Empty, S(x, "name") ?? "Business Meta")).Where(x => !string.IsNullOrWhiteSpace(x.Id)).ToArray();
    }

    public async Task<IReadOnlyList<MetaAdsAdAccountResponse>> ListAdAccountsAsync(MetaAdsConfiguration config, string accessToken, string businessId, CancellationToken cancellationToken)
    {
        var fields = "id,account_id,name,account_status,currency";
        var owned = await GetPagedDataAsync(GraphUrl(config, $"{businessId}/owned_ad_accounts", new() { ["fields"] = fields, ["limit"] = "100" }), accessToken, cancellationToken);
        var client = await GetPagedDataAsync(GraphUrl(config, $"{businessId}/client_ad_accounts", new() { ["fields"] = fields, ["limit"] = "100" }), accessToken, cancellationToken);
        return owned.Concat(client)
            .Select(ToAdAccount)
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToArray();
    }

    public async Task<IReadOnlyList<MetaAdsPageResponse>> ListPagesAsync(MetaAdsConfiguration config, string accessToken, CancellationToken cancellationToken)
    {
        var url = GraphUrl(config, "me/accounts", new() { ["fields"] = "id,name,instagram_business_account{id,name,username}", ["limit"] = "100" });
        var rows = await GetPagedDataAsync(url, accessToken, cancellationToken);
        return rows.Select(ToPage).Where(x => !string.IsNullOrWhiteSpace(x.Id)).ToArray();
    }

    public async Task<MetaAdsPageResponse?> GetPageAsync(MetaAdsConfiguration config, string accessToken, string pageId, CancellationToken cancellationToken)
    {
        using var root = await GetJsonAsync(GraphUrl(config, pageId, new() { ["fields"] = "id,name,instagram_business_account{id,name,username}" }), accessToken, cancellationToken);
        return root.RootElement.ValueKind == JsonValueKind.Object ? ToPage(root.RootElement) : null;
    }

    public async Task<IReadOnlyList<MetaAdsPixelResponse>> ListPixelsAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, CancellationToken cancellationToken)
    {
        var normalized = NormalizeAdAccountId(adAccountId);
        var url = GraphUrl(config, $"{normalized}/adspixels", new() { ["fields"] = "id,name", ["limit"] = "100" });
        var rows = await GetPagedDataAsync(url, accessToken, cancellationToken);
        return rows.Select(x => new MetaAdsPixelResponse(S(x, "id") ?? string.Empty, S(x, "name") ?? "Pixel Meta")).Where(x => !string.IsNullOrWhiteSpace(x.Id)).ToArray();
    }

    public async Task<MetaAdsPermissionStatusResponse> GetPermissionsAsync(MetaAdsConfiguration config, string accessToken, CancellationToken cancellationToken)
    {
        var rows = await GetPagedDataAsync(GraphUrl(config, "me/permissions", new() { ["limit"] = "100" }), accessToken, cancellationToken);
        var permissions = new List<MetaAdsPermissionResponse>();
        foreach (var item in rows)
        {
            var permission = S(item, "permission");
            var status = S(item, "status");
            if (string.IsNullOrWhiteSpace(permission))
            {
                continue;
            }

            if (string.Equals(status, "granted", StringComparison.OrdinalIgnoreCase))
            {
                permissions.Add(new MetaAdsPermissionResponse(permission, "Granted"));
            }
            else if (string.Equals(status, "declined", StringComparison.OrdinalIgnoreCase))
            {
                permissions.Add(new MetaAdsPermissionResponse(permission, "Declined"));
            }
            else if (string.Equals(status, "expired", StringComparison.OrdinalIgnoreCase))
            {
                permissions.Add(new MetaAdsPermissionResponse(permission, "Expired"));
            }
            else permissions.Add(new MetaAdsPermissionResponse(permission, status ?? "Unknown"));
        }

        return new MetaAdsPermissionStatusResponse(permissions.GroupBy(x => x.Permission, StringComparer.OrdinalIgnoreCase).Select(x => x.First()).ToArray());
    }

    public async Task<IReadOnlyList<MetaAdsLocationResponse>> SearchTargetingLocationsAsync(MetaAdsConfiguration config, string accessToken, string query, string countryCode, int limit, CancellationToken cancellationToken)
    {
        var url = GraphUrl(config, "search", new()
        {
            ["type"] = "adgeolocation",
            ["location_types"] = "[\"country\",\"region\",\"city\"]",
            ["q"] = query,
            ["country_code"] = countryCode,
            ["limit"] = Math.Clamp(limit, 1, 25).ToString(System.Globalization.CultureInfo.InvariantCulture)
        });
        var rows = await GetPagedDataAsync(url, accessToken, cancellationToken);
        return rows.Select(ToLocation).Where(x => !string.IsNullOrWhiteSpace(x.Key)).ToArray();
    }

    public async Task<string> UploadAdImageAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, string fileName, string contentType, byte[] content, CancellationToken cancellationToken)
    {
        var normalized = NormalizeAdAccountId(adAccountId);
        using var request = new HttpRequestMessage(HttpMethod.Post, GraphUrl(config, $"{normalized}/adimages", new()));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(fileName), "filename");
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "source", fileName);
        request.Content = form;

        using var response = await httpClientFactory.CreateClient("metaads").SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            throw ParseError(text, response.StatusCode, "adimages");
        }

        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        if (json.RootElement.TryGetProperty("images", out var images) && images.ValueKind == JsonValueKind.Object)
        {
            foreach (var image in images.EnumerateObject())
            {
                var hash = S(image.Value, "hash");
                if (!string.IsNullOrWhiteSpace(hash))
                {
                    return hash;
                }
            }
        }

        throw new MetaAdsGraphApiException("Upload de imagem Meta nao retornou image_hash.", "meta_image_hash_missing", false, response.StatusCode);
    }

    public async Task<bool> ResourceExistsAsync(MetaAdsConfiguration config, string accessToken, string resourceId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, GraphUrl(config, resourceId, new() { ["fields"] = "id" }));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await httpClientFactory.CreateClient("metaads").SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        if ((int)response.StatusCode == 404 || response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return false;
        }

        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        throw ParseError(text, response.StatusCode);
    }

    public async Task<MetaAdsCreateResult> CreateCampaignAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, MetaAdsCampaignCreatePayload payload, CancellationToken cancellationToken)
    {
        var fieldNames = string.IsNullOrWhiteSpace(payload.BidStrategy)
            ? "name,objective,buying_type,special_ad_categories,status,is_adset_budget_sharing_enabled"
            : "name,objective,buying_type,special_ad_categories,bid_strategy,status,is_adset_budget_sharing_enabled";

        logger.LogInformation(
            "Meta Campaign create request fields. Edge={MetaEdge} FieldNames={FieldNames} Name={CampaignName} Objective={Objective} BuyingType={BuyingType} SpecialAdCategories={SpecialAdCategories} BidStrategy={BidStrategy} Status={Status} IsAdsetBudgetSharingEnabled={IsAdsetBudgetSharingEnabled}",
            "campaigns",
            fieldNames,
            SanitizeMetaMessage(payload.Name),
            payload.Objective,
            MetaAdsConstants.BuyingTypeAuction,
            JsonSerializer.Serialize(payload.SpecialAdCategories),
            payload.BidStrategy,
            MetaAdsConstants.StatusPaused,
            MetaAdsConstants.IsAdsetBudgetSharingEnabled);

        var values = new Dictionary<string, string>
        {
            ["name"] = payload.Name,
            ["objective"] = payload.Objective,
            ["buying_type"] = MetaAdsConstants.BuyingTypeAuction,
            ["special_ad_categories"] = JsonSerializer.Serialize(payload.SpecialAdCategories),
            ["status"] = MetaAdsConstants.StatusPaused,
            ["is_adset_budget_sharing_enabled"] = MetaAdsConstants.IsAdsetBudgetSharingEnabled ? "true" : "false"
        };
        if (!string.IsNullOrWhiteSpace(payload.BidStrategy))
        {
            values["bid_strategy"] = payload.BidStrategy;
        }

        return await PostFormForIdAsync(config, accessToken, adAccountId, "campaigns", values, cancellationToken);
    }

    public async Task DeleteCampaignAsync(MetaAdsConfiguration config, string accessToken, string campaignId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, GraphUrl(config, campaignId, new()));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await httpClientFactory.CreateClient("metaads").SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            throw ParseError(text, response.StatusCode, "campaigns");
        }

        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        if (!json.RootElement.TryGetProperty("success", out var success) || success.ValueKind != JsonValueKind.True)
        {
            throw new MetaAdsGraphApiException("Exclusao de campanha Meta nao retornou success=true.", "meta_delete_not_confirmed", false, response.StatusCode);
        }
    }

    public async Task<MetaAdsCreateResult> CreateAdSetAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, MetaAdsAdSetCreatePayload payload, CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, string>
        {
            ["name"] = payload.Name,
            ["campaign_id"] = payload.CampaignId,
            ["optimization_goal"] = payload.OptimizationGoal,
            ["billing_event"] = payload.BillingEvent,
            ["daily_budget"] = payload.DailyBudget.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["targeting"] = JsonSerializer.Serialize(ToTargetingJson(payload.Targeting)),
            ["status"] = MetaAdsConstants.StatusPaused
        };

        if (!string.IsNullOrWhiteSpace(payload.BidStrategy))
        {
            values["bid_strategy"] = payload.BidStrategy;
        }
        if (payload.StartTime is not null)
        {
            values["start_time"] = ToMetaDateTime(payload.StartTime.Value);
        }
        if (payload.EndTime is not null)
        {
            values["end_time"] = ToMetaDateTime(payload.EndTime.Value);
        }

        return await PostFormForIdAsync(config, accessToken, adAccountId, "adsets", values, cancellationToken);
    }

    public async Task DeleteAdSetAsync(MetaAdsConfiguration config, string accessToken, string adSetId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, GraphUrl(config, adSetId, new()));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await httpClientFactory.CreateClient("metaads").SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            throw ParseError(text, response.StatusCode, "adsets");
        }

        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        if (!json.RootElement.TryGetProperty("success", out var success) || success.ValueKind != JsonValueKind.True)
        {
            throw new MetaAdsGraphApiException("Exclusao de Ad Set Meta nao retornou success=true.", "meta_delete_not_confirmed", false, response.StatusCode);
        }
    }

    public async Task<MetaAdsCreateResult> CreateAdCreativeAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, MetaAdsCreativeCreatePayload payload, CancellationToken cancellationToken)
    {
        var spec = new Dictionary<string, object?>
        {
            ["page_id"] = payload.PageId,
            ["link_data"] = new Dictionary<string, object?>
            {
                ["image_hash"] = payload.ImageHash,
                ["link"] = payload.Link,
                ["message"] = payload.Message,
                ["name"] = payload.Headline,
                ["description"] = payload.Description,
                ["call_to_action"] = new Dictionary<string, object?>
                {
                    ["type"] = payload.CallToAction,
                    ["value"] = new Dictionary<string, object?> { ["link"] = payload.Link }
                }
            }
        };
        if (!string.IsNullOrWhiteSpace(payload.InstagramActorId))
        {
            spec["instagram_actor_id"] = payload.InstagramActorId;
        }

        logger.LogInformation(
            "Meta Ad Creative create request. Edge={MetaEdge} AdAccountId={AdAccountId} PageId={PageId} ImageHash={ImageHash} Link={Link} Message={Message} Name={Headline} Description={Description} CallToActionType={CallToActionType}",
            "adcreatives",
            NormalizeAdAccountId(adAccountId),
            payload.PageId,
            payload.ImageHash,
            SanitizeMetaMessage(payload.Link),
            SanitizeMetaMessage(payload.Message),
            SanitizeMetaMessage(payload.Headline),
            SanitizeMetaMessage(payload.Description),
            payload.CallToAction);

        return await PostFormForIdAsync(config, accessToken, adAccountId, "adcreatives", new()
        {
            ["name"] = payload.Name,
            ["object_story_spec"] = JsonSerializer.Serialize(spec)
        }, cancellationToken);
    }

    public async Task<MetaAdsCreateResult> CreateDiagnosticAdCreativeAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, MetaAdsDiagnosticCreativeCreatePayload payload, CancellationToken cancellationToken)
    {
        var linkData = new Dictionary<string, object?>
        {
            ["image_hash"] = payload.ImageHash,
            ["link"] = payload.Link,
            ["message"] = payload.Message,
            ["name"] = payload.Headline
        };
        if (!string.IsNullOrWhiteSpace(payload.Description))
        {
            linkData["description"] = payload.Description;
        }
        if (!string.IsNullOrWhiteSpace(payload.CallToAction))
        {
            linkData["call_to_action"] = new Dictionary<string, object?>
            {
                ["type"] = payload.CallToAction,
                ["value"] = new Dictionary<string, object?> { ["link"] = payload.Link }
            };
        }

        var spec = new Dictionary<string, object?>
        {
            ["page_id"] = payload.PageId,
            ["link_data"] = linkData
        };

        return await PostFormForIdAsync(config, accessToken, adAccountId, "adcreatives", new()
        {
            ["name"] = payload.Name,
            ["object_story_spec"] = JsonSerializer.Serialize(spec)
        }, cancellationToken);
    }

    public async Task DeleteAdCreativeAsync(MetaAdsConfiguration config, string accessToken, string creativeId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, GraphUrl(config, creativeId, new()));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await httpClientFactory.CreateClient("metaads").SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            throw ParseError(text, response.StatusCode, "adcreatives");
        }

        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        if (!json.RootElement.TryGetProperty("success", out var success) || success.ValueKind != JsonValueKind.True)
        {
            throw new MetaAdsGraphApiException("Exclusao de Creative Meta nao retornou success=true.", "meta_delete_not_confirmed", false, response.StatusCode);
        }
    }

    public async Task<MetaAdsCreateResult> CreateAdAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, MetaAdsAdCreatePayload payload, CancellationToken cancellationToken)
    {
        return await PostFormForIdAsync(config, accessToken, adAccountId, "ads", new()
        {
            ["name"] = payload.Name,
            ["adset_id"] = payload.AdSetId,
            ["creative"] = JsonSerializer.Serialize(new Dictionary<string, string> { ["creative_id"] = payload.CreativeId }),
            ["status"] = "PAUSED"
        }, cancellationToken);
    }

    public async Task DeleteAdAsync(MetaAdsConfiguration config, string accessToken, string adId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, GraphUrl(config, adId, new()));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await httpClientFactory.CreateClient("metaads").SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            throw ParseError(text, response.StatusCode, "ads");
        }

        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        if (!json.RootElement.TryGetProperty("success", out var success) || success.ValueKind != JsonValueKind.True)
        {
            throw new MetaAdsGraphApiException("Exclusao de Ad Meta nao retornou success=true.", "meta_delete_not_confirmed", false, response.StatusCode);
        }
    }

    private async Task<IReadOnlyList<JsonElement>> GetPagedDataAsync(string initialUrl, string accessToken, CancellationToken cancellationToken)
    {
        var result = new List<JsonElement>();
        var url = initialUrl;
        for (var page = 0; page < MaxPages && !string.IsNullOrWhiteSpace(url); page++)
        {
            using var json = await GetJsonAsync(url, accessToken, cancellationToken);
            if (!json.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            foreach (var item in data.EnumerateArray())
            {
                result.Add(item.Clone());
            }

            url = null;
            if (json.RootElement.TryGetProperty("paging", out var paging)
                && paging.TryGetProperty("next", out var next)
                && next.ValueKind == JsonValueKind.String)
            {
                url = next.GetString();
            }
        }

        return result;
    }

    private async Task<JsonDocument> GetJsonAsync(string url, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await httpClientFactory.CreateClient("metaads").SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            throw ParseError(text, response.StatusCode);
        }

        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
    }

    private async Task<MetaAdsCreateResult> PostFormForIdAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, string edge, Dictionary<string, string> values, CancellationToken cancellationToken)
    {
        var normalized = NormalizeAdAccountId(adAccountId);
        using var request = new HttpRequestMessage(HttpMethod.Post, GraphUrl(config, $"{normalized}/{edge}", new()));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new FormUrlEncodedContent(values);
        using var response = await httpClientFactory.CreateClient("metaads").SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            throw ParseError(text, response.StatusCode, edge);
        }

        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var id = S(json.RootElement, "id");
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new MetaAdsGraphApiException($"Resposta Meta sem ID ao criar {edge}.", "meta_create_missing_id", false, response.StatusCode);
        }

        return new MetaAdsCreateResult(id);
    }

    private static Dictionary<string, object?> ToTargetingJson(MetaAdsTargetingCreatePayload targeting)
    {
        var geo = new Dictionary<string, object?>();
        if (targeting.Countries.Count > 0)
        {
            geo["countries"] = targeting.Countries;
        }
        if (targeting.Regions.Count > 0)
        {
            geo["regions"] = targeting.Regions.Select(x => new Dictionary<string, string> { ["key"] = x.Key }).ToArray();
        }
        if (targeting.Cities.Count > 0)
        {
            geo["cities"] = targeting.Cities.Select(x => new Dictionary<string, string> { ["key"] = x.Key }).ToArray();
        }

        var result = new Dictionary<string, object?>
        {
            ["geo_locations"] = geo
        };
        if (targeting.AgeMin is not null)
        {
            result["age_min"] = targeting.AgeMin;
        }
        if (targeting.AgeMax is not null)
        {
            result["age_max"] = targeting.AgeMax;
        }
        if (targeting.Genders?.Count > 0)
        {
            result["genders"] = targeting.Genders;
        }
        if (targeting.AdvantageAudience is not null)
        {
            result["targeting_automation"] = new Dictionary<string, int>
            {
                ["advantage_audience"] = targeting.AdvantageAudience.Value
            };
        }

        return result;
    }

    private static string ToMetaDateTime(DateTimeOffset value)
    {
        var utc = value.ToUniversalTime();
        return utc.ToString("yyyy-MM-dd'T'HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) + "+0000";
    }

    private MetaAdsGraphApiException ParseError(string body, System.Net.HttpStatusCode? statusCode = null, string? edge = null)
    {
        try
        {
            using var json = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            if (json.RootElement.TryGetProperty("error", out var error))
            {
                var code = error.TryGetProperty("code", out var codeValue) ? codeValue.ToString() : "meta_api_error";
                var subcode = error.TryGetProperty("error_subcode", out var subcodeValue) ? subcodeValue.ToString() : null;
                var type = S(error, "type") ?? "erro";
                var trace = S(error, "fbtrace_id");
                var metaMessage = SanitizeMetaMessage(S(error, "message"));
                var errorUserTitle = SanitizeMetaMessage(S(error, "error_user_title"));
                var errorUserMessage = SanitizeMetaMessage(S(error, "error_user_msg"));
                var errorData = SanitizedJson(error, "error_data");
                var blameField = SanitizedNestedJson(error, "error_data", "blame_field");
                var blameFieldSpecs = SanitizedNestedJson(error, "error_data", "blame_field_specs");
                var isTransient = error.TryGetProperty("is_transient", out var transientValue) && transientValue.ValueKind is JsonValueKind.True or JsonValueKind.False
                    ? transientValue.GetBoolean()
                    : (bool?)null;
                var permission = code is "10" or "200" || string.Equals(type, "OAuthException", StringComparison.OrdinalIgnoreCase);
                LogMetaError(edge, statusCode, type, code, subcode, trace, metaMessage, errorUserTitle, errorUserMessage, errorData, blameField, blameFieldSpecs, isTransient);
                var message = string.IsNullOrWhiteSpace(metaMessage) ? $"Falha Graph API Meta ({type} {code})." : metaMessage;
                return new MetaAdsGraphApiException(message, code, permission, statusCode, subcode, type, trace, metaMessage, errorUserTitle, errorUserMessage, errorData, blameField, blameFieldSpecs, isTransient);
            }
        }
        catch (JsonException)
        {
            LogMetaError(edge, statusCode, null, "meta_api_error", null, null, "Resposta de erro Meta nao estava em JSON valido.", null, null, null, null, null, null);
            return new MetaAdsGraphApiException("Falha ao consultar Graph API Meta.", "meta_api_error", false, statusCode);
        }

        LogMetaError(edge, statusCode, null, "meta_api_error", null, null, "Resposta de erro Meta sem objeto error.", null, null, null, null, null, null);
        return new MetaAdsGraphApiException("Falha ao consultar Graph API Meta.", "meta_api_error", false, statusCode);
    }

    private void LogMetaError(
        string? edge,
        System.Net.HttpStatusCode? statusCode,
        string? type,
        string? code,
        string? subcode,
        string? trace,
        string? message,
        string? errorUserTitle,
        string? errorUserMessage,
        string? errorData,
        string? blameField,
        string? blameFieldSpecs,
        bool? isTransient)
    {
        logger.LogWarning(
            "Meta Graph API error. Edge={MetaEdge} HttpStatus={HttpStatus} MetaErrorMessage={MetaErrorMessage} MetaErrorType={MetaErrorType} MetaErrorCode={MetaErrorCode} MetaErrorSubcode={MetaErrorSubcode} MetaErrorUserTitle={MetaErrorUserTitle} MetaErrorUserMessage={MetaErrorUserMessage} FbTraceId={FbTraceId} ErrorData={ErrorData} BlameField={BlameField} BlameFieldSpecs={BlameFieldSpecs} IsTransient={IsTransient}",
            edge,
            statusCode?.ToString(),
            message,
            type,
            code,
            subcode,
            errorUserTitle,
            errorUserMessage,
            trace,
            errorData,
            blameField,
            blameFieldSpecs,
            isTransient);
    }

    private static string? SanitizedJson(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value))
        {
            return null;
        }

        return SanitizeMetaMessage(value.ToString());
    }

    private static string? SanitizedNestedJson(JsonElement element, string parent, string property)
    {
        if (!element.TryGetProperty(parent, out var parentValue)
            || parentValue.ValueKind != JsonValueKind.Object
            || !parentValue.TryGetProperty(property, out var value))
        {
            return null;
        }

        return SanitizeMetaMessage(value.ToString());
    }

    private static string? SanitizeMetaMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        var sanitized = message
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace("\t", " ")
            .Trim();
        sanitized = Regex.Replace(sanitized, "(access_token|client_secret|appsecret_proof)=([^\\s&]+)", "$1=[redacted]", RegexOptions.IgnoreCase);
        sanitized = Regex.Replace(sanitized, "Bearer\\s+[^\\s]+", "Bearer [redacted]", RegexOptions.IgnoreCase);

        return sanitized.Length <= 500 ? sanitized : sanitized[..500];
    }

    private static MetaAdsAdAccountResponse ToAdAccount(JsonElement item)
    {
        return new MetaAdsAdAccountResponse(
            S(item, "id") ?? string.Empty,
            S(item, "account_id"),
            S(item, "name") ?? "Conta de anuncios Meta",
            S(item, "account_status"),
            S(item, "currency"));
    }

    private static MetaAdsPageResponse ToPage(JsonElement item)
    {
        MetaAdsInstagramAccountResponse? instagram = null;
        if (item.TryGetProperty("instagram_business_account", out var ig) && ig.ValueKind == JsonValueKind.Object)
        {
            instagram = new MetaAdsInstagramAccountResponse(S(ig, "id") ?? string.Empty, S(ig, "name"), S(ig, "username"));
        }

        return new MetaAdsPageResponse(S(item, "id") ?? string.Empty, S(item, "name") ?? "Pagina Meta", instagram);
    }

    private static MetaAdsLocationResponse ToLocation(JsonElement item)
    {
        var type = S(item, "type") ?? string.Empty;
        return new MetaAdsLocationResponse(
            S(item, "key") ?? string.Empty,
            S(item, "name") ?? "Localizacao Meta",
            type,
            S(item, "country_code"),
            S(item, "country_name"),
            S(item, "region"),
            S(item, "region_id"),
            string.Equals(type, "region", StringComparison.OrdinalIgnoreCase),
            string.Equals(type, "city", StringComparison.OrdinalIgnoreCase));
    }

    private static string GraphUrl(MetaAdsConfiguration config, string path, Dictionary<string, string?> query)
    {
        var baseUrl = $"{config.GraphApiBaseUrl.TrimEnd('/')}/{config.GraphApiVersion.Trim('/')}/{path.TrimStart('/')}";
        return AddQueryString(baseUrl, query);
    }

    private static string AddQueryString(string url, Dictionary<string, string?> values)
    {
        var separator = url.Contains('?') ? "&" : "?";
        return url + separator + string.Join("&", values
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value!)}"));
    }

    private static string? S(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) ? value.ToString() : null;
    }

    private static string NormalizeAdAccountId(string adAccountId)
    {
        return adAccountId.StartsWith("act_", StringComparison.OrdinalIgnoreCase) ? adAccountId : $"act_{adAccountId}";
    }
}
