using System.Net;
using System.Text;
using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Enums;
using LeadEngine.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace LeadEngine.Application.Tests;

public sealed class MetaAdsGraphClientTests
{
    [Fact]
    public async Task GetAdAccountAsync_LeConta()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
        { "id": "act_1668410610924666", "name": "LeadEngine", "account_status": 1, "currency": "BRL", "timezone_name": "America/Sao_Paulo" }
        """));
        var client = Client(handler);

        var result = await client.GetAdAccountAsync(Config(), "token-secreto", "act_1668410610924666", CancellationToken.None);

        Assert.Equal("act_1668410610924666", result.Id);
        Assert.Equal("LeadEngine", result.Name);
        Assert.Equal("1", result.AccountStatus);
        Assert.Equal("BRL", result.Currency);
        Assert.Equal("America/Sao_Paulo", result.TimezoneName);
        Assert.DoesNotContain("access_token", handler.LastRequestUri);
    }

    [Fact]
    public async Task GetCampaignsAsync_ParseiaLista()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
        { "data": [{ "id": "111", "name": "Campanha", "status": "PAUSED", "effective_status": "PAUSED", "bid_strategy": "LOWEST_COST_WITHOUT_CAP" }] }
        """));
        var client = Client(handler);

        var result = await client.GetCampaignsAsync(Config(), "token-secreto", "1668410610924666", CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal("111", item.Id);
        Assert.Equal("Campanha", item.Name);
        Assert.Equal("PAUSED", item.Status);
        Assert.Equal("PAUSED", item.EffectiveStatus);
        Assert.Equal("LOWEST_COST_WITHOUT_CAP", item.BidStrategy);
        Assert.Contains("/v23.0/act_1668410610924666/campaigns", handler.LastRequestUri);
        Assert.Contains("bid_strategy", handler.LastRequestUri);
    }

    [Fact]
    public async Task CreateCampaignAsync_ForcaPausedMesmoSePayloadVierActive()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{ "id": "2381" }"""));
        var client = Client(handler);

        await client.CreateCampaignAsync(
            Config(),
            "token-secreto",
            "act_1668410610924666",
            new MetaAdsCampaignCreatePayload("LeadEngine - Teste", MetaAdsConstants.ObjectiveOutcomeLeads, [], "ACTIVE"),
            CancellationToken.None);

        Assert.Contains("status=PAUSED", handler.LastRequestBody);
        Assert.DoesNotContain("status=ACTIVE", handler.LastRequestBody);
    }

    [Fact]
    public async Task CreateCampaignAsync_EnviaIsAdsetBudgetSharingEnabledFalse()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{ "id": "2381" }"""));
        var client = Client(handler);

        await client.CreateCampaignAsync(
            Config(),
            "token-secreto",
            "act_1668410610924666",
            new MetaAdsCampaignCreatePayload("LeadEngine - Teste", MetaAdsConstants.ObjectiveOutcomeLeads, [], MetaAdsConstants.StatusPaused),
            CancellationToken.None);

        Assert.Contains("is_adset_budget_sharing_enabled=false", handler.LastRequestBody);
        Assert.Contains("special_ad_categories=%5B%5D", handler.LastRequestBody);
        Assert.DoesNotContain("bid_strategy", handler.LastRequestBody);
    }

    [Fact]
    public async Task DiagnosticsService_CreateCampaign_NaoEnviaBidStrategyNemBidAmount()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{ "id": "2381" }"""));
        var service = new MetaAdsDiagnosticsService(new FakeResolver(), Client(handler));

        await service.CreateCampaignAsync(new CreateMetaCampaignRequest("LeadEngine - Teste"), CancellationToken.None);

        var body = Decode(handler.LastRequestBody);
        Assert.Contains("objective=OUTCOME_LEADS", body);
        Assert.Contains("buying_type=AUCTION", body);
        Assert.Contains("status=PAUSED", body);
        Assert.Contains("is_adset_budget_sharing_enabled=false", body);
        Assert.DoesNotContain("bid_strategy", body);
        Assert.DoesNotContain("bid_amount", body);
        Assert.DoesNotContain("bid_constraints", body);
        Assert.DoesNotContain("cost_cap", body);
        Assert.DoesNotContain("roas", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ErroHttpMeta_ViraExcecaoComDiagnostico()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
        {
          "error": {
            "message": "Invalid parameter access_token=token-secreto",
            "type": "OAuthException",
            "code": 100,
            "error_subcode": 4834011,
            "fbtrace_id": "trace-1"
          }
        }
        """, HttpStatusCode.BadRequest));
        var client = Client(handler);

        var ex = await Assert.ThrowsAsync<MetaAdsGraphApiException>(() =>
            client.GetCampaignsAsync(Config(), "token-secreto", "act_1668410610924666", CancellationToken.None));

        Assert.Equal("100", ex.Code);
        Assert.Equal("4834011", ex.ErrorSubcode);
        Assert.Equal("trace-1", ex.FbTraceId);
        Assert.Equal(HttpStatusCode.BadRequest, ex.HttpStatusCode);
    }

    [Fact]
    public async Task ErroHttpMeta_NaoExpoeAccessTokenNaMensagem()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
        {
          "error": {
            "message": "Invalid OAuth access_token=token-secreto",
            "type": "OAuthException",
            "code": 190,
            "fbtrace_id": "trace-2"
          }
        }
        """, HttpStatusCode.BadRequest));
        var client = Client(handler);

        var ex = await Assert.ThrowsAsync<MetaAdsGraphApiException>(() =>
            client.GetAdAccountAsync(Config(), "token-secreto", "act_1668410610924666", CancellationToken.None));

        Assert.DoesNotContain("token-secreto", ex.Message);
        Assert.Contains("[redacted]", ex.Message);
    }

    [Fact]
    public async Task DeleteCampaignAsync_SuccessTrueConclui()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{ "success": true }"""));
        var client = Client(handler);

        await client.DeleteCampaignAsync(Config(), "token-secreto", "2381", CancellationToken.None);

        Assert.Equal(HttpMethod.Delete, handler.LastMethod);
        Assert.Contains("/v23.0/2381", handler.LastRequestUri);
    }

    [Fact]
    public async Task CreateAdSetAsync_EnviaPayloadMinimoComPausedETargeting()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{ "id": "adset_1" }"""));
        var client = Client(handler);

        await client.CreateAdSetAsync(
            Config(),
            "token-secreto",
            "act_1668410610924666",
            new MetaAdsAdSetCreatePayload(
                "LeadEngine - AdSet",
                "campaign_1",
                MetaAdsConstants.OptimizationGoalLeadGeneration,
                MetaAdsConstants.BillingEventImpressions,
                2000,
                null,
                new MetaAdsTargetingCreatePayload(["BR"], [], [], 25, 60, [1]),
                "ACTIVE",
                new DateTimeOffset(2026, 8, 22, 12, 30, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 8, 23, 12, 30, 0, TimeSpan.Zero)),
            CancellationToken.None);

        var body = Decode(handler.LastRequestBody);
        Assert.Contains("campaign_id=campaign_1", body);
        Assert.Contains("name=LeadEngine - AdSet", body);
        Assert.Contains("status=PAUSED", body);
        Assert.DoesNotContain("status=ACTIVE", body);
        Assert.Contains("daily_budget=2000", body);
        Assert.Contains("billing_event=IMPRESSIONS", body);
        Assert.Contains("optimization_goal=LEAD_GENERATION", body);
        Assert.Contains("\"countries\":[\"BR\"]", body);
        Assert.Contains("\"age_min\":25", body);
        Assert.Contains("\"age_max\":60", body);
        Assert.Contains("\"genders\":[1]", body);
        Assert.Contains("start_time=2026-08-22T12:30:00+0000", body);
        Assert.Contains("end_time=2026-08-23T12:30:00+0000", body);
        Assert.DoesNotContain("bid_strategy", body);
    }

    [Fact]
    public async Task CreateAdSetAsync_EnviaBidStrategyQuandoPayloadExistenteInformar()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{ "id": "adset_1" }"""));
        var client = Client(handler);

        await client.CreateAdSetAsync(
            Config(),
            "token-secreto",
            "act_1668410610924666",
            new MetaAdsAdSetCreatePayload(
                "LeadEngine - AdSet",
                "campaign_1",
                "LINK_CLICKS",
                MetaAdsConstants.BillingEventImpressions,
                2000,
                MetaAdsConstants.BidStrategyLowestCostWithoutCap,
                new MetaAdsTargetingCreatePayload(["BR"], [], [], 25, 60),
                MetaAdsConstants.StatusPaused),
            CancellationToken.None);

        Assert.Contains("bid_strategy=LOWEST_COST_WITHOUT_CAP", Decode(handler.LastRequestBody));
    }

    [Fact]
    public async Task CreateAdSetAsync_ErroMetaConvertidoESanitizado()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
        {
          "error": {
            "message": "Invalid OAuth access_token=token-secreto",
            "type": "OAuthException",
            "code": 100,
            "error_subcode": 1815753,
            "fbtrace_id": "trace-adset"
          }
        }
        """, HttpStatusCode.BadRequest));
        var client = Client(handler);

        var ex = await Assert.ThrowsAsync<MetaAdsGraphApiException>(() =>
            client.CreateAdSetAsync(
                Config(),
                "token-secreto",
                "act_1668410610924666",
                new MetaAdsAdSetCreatePayload(
                    "LeadEngine - AdSet",
                    "campaign_1",
                    MetaAdsConstants.OptimizationGoalLeadGeneration,
                    MetaAdsConstants.BillingEventImpressions,
                    2000,
                    null,
                    new MetaAdsTargetingCreatePayload(["BR"], [], [], null, null),
                    MetaAdsConstants.StatusPaused),
                CancellationToken.None));

        Assert.Equal("100", ex.Code);
        Assert.Equal("1815753", ex.ErrorSubcode);
        Assert.Equal("trace-adset", ex.FbTraceId);
        Assert.DoesNotContain("token-secreto", ex.Message);
    }

    [Fact]
    public async Task DeleteAdSetAsync_SuccessTrueConclui()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{ "success": true }"""));
        var client = Client(handler);

        await client.DeleteAdSetAsync(Config(), "token-secreto", "adset_1", CancellationToken.None);

        Assert.Equal(HttpMethod.Delete, handler.LastMethod);
        Assert.Contains("/v23.0/adset_1", handler.LastRequestUri);
    }

    [Fact]
    public async Task GetAdCreativesAsync_ParseiaLista()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
        { "data": [{ "id": "creative_1", "name": "Creative", "status": "ACTIVE", "object_story_id": "story_1", "object_story_spec": { "page_id": "page_1" } }] }
        """));
        var client = Client(handler);

        var result = await client.GetAdCreativesAsync(Config(), "token-secreto", "1668410610924666", CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal("creative_1", item.Id);
        Assert.Equal("Creative", item.Name);
        Assert.Equal("ACTIVE", item.Status);
        Assert.Equal("story_1", item.ObjectStoryId);
        Assert.Contains("\"page_id\"", item.ObjectStorySpec);
        Assert.Contains("\"page_1\"", item.ObjectStorySpec);
        Assert.Contains("/v23.0/act_1668410610924666/adcreatives", handler.LastRequestUri);
        Assert.DoesNotContain("access_token", handler.LastRequestUri);
    }

    [Fact]
    public async Task CreateDiagnosticAdCreativeAsync_EnviaPayloadMinimoSemInstagramSemCtaSemDescription()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{ "id": "creative_1" }"""));
        var client = Client(handler);

        await client.CreateDiagnosticAdCreativeAsync(
            Config(),
            "token-secreto",
            "act_1668410610924666",
            new MetaAdsDiagnosticCreativeCreatePayload(
                "LeadEngine - Creative",
                "page_1",
                "image_hash_1",
                "https://example.com/landing",
                "Texto principal",
                "Titulo",
                null,
                null),
            CancellationToken.None);

        var body = Decode(handler.LastRequestBody);
        Assert.Contains("name=LeadEngine - Creative", body);
        Assert.Contains("\"page_id\":\"page_1\"", body);
        Assert.Contains("\"image_hash\":\"image_hash_1\"", body);
        Assert.Contains("\"link\":\"https://example.com/landing\"", body);
        Assert.Contains("\"message\":\"Texto principal\"", body);
        Assert.Contains("\"name\":\"Titulo\"", body);
        Assert.DoesNotContain("instagram_actor_id", body);
        Assert.DoesNotContain("description", body);
        Assert.DoesNotContain("call_to_action", body);
        Assert.DoesNotContain("access_token", handler.LastRequestUri);
    }

    [Fact]
    public async Task CreateDiagnosticAdCreativeAsync_EnviaDescriptionECtaLearnMoreQuandoInformados()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{ "id": "creative_1" }"""));
        var client = Client(handler);

        await client.CreateDiagnosticAdCreativeAsync(
            Config(),
            "token-secreto",
            "act_1668410610924666",
            new MetaAdsDiagnosticCreativeCreatePayload(
                "LeadEngine - Creative",
                "page_1",
                "image_hash_1",
                "https://example.com/landing",
                "Texto principal",
                "Titulo",
                "Descricao",
                "LEARN_MORE"),
            CancellationToken.None);

        var body = Decode(handler.LastRequestBody);
        Assert.Contains("\"description\":\"Descricao\"", body);
        Assert.Contains("\"call_to_action\":{\"type\":\"LEARN_MORE\",\"value\":{\"link\":\"https://example.com/landing\"}}", body);
    }

    [Fact]
    public async Task CreateAdCreativeAsync_FluxoAntigoPreservado()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{ "id": "creative_1" }"""));
        var client = Client(handler);

        await client.CreateAdCreativeAsync(
            Config(),
            "token-secreto",
            "act_1668410610924666",
            new MetaAdsCreativeCreatePayload(
                "LeadEngine - Creative",
                "page_1",
                "ig_1",
                "image_hash_1",
                "https://example.com/landing",
                "Texto principal",
                "Titulo",
                "Descricao",
                "LEARN_MORE"),
            CancellationToken.None);

        var body = Decode(handler.LastRequestBody);
        Assert.Contains("\"page_id\":\"page_1\"", body);
        Assert.Contains("\"instagram_actor_id\":\"ig_1\"", body);
        Assert.Contains("\"link_data\":{\"image_hash\":\"image_hash_1\",\"link\":\"https://example.com/landing\",\"message\":\"Texto principal\",\"name\":\"Titulo\",\"description\":\"Descricao\",\"call_to_action\":{\"type\":\"LEARN_MORE\",\"value\":{\"link\":\"https://example.com/landing\"}}}", body);
    }

    [Fact]
    public async Task CreateDiagnosticAdCreativeAsync_ErroMetaConvertidoESanitizado()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
        {
          "error": {
            "message": "Invalid OAuth access_token=token-secreto",
            "type": "OAuthException",
            "code": 100,
            "error_subcode": 1885316,
            "error_user_title": "Creative invalido",
            "error_user_msg": "Revise os campos.",
            "fbtrace_id": "trace-creative"
          }
        }
        """, HttpStatusCode.BadRequest));
        var client = Client(handler);

        var ex = await Assert.ThrowsAsync<MetaAdsGraphApiException>(() =>
            client.CreateDiagnosticAdCreativeAsync(
                Config(),
                "token-secreto",
                "act_1668410610924666",
                new MetaAdsDiagnosticCreativeCreatePayload(
                    "LeadEngine - Creative",
                    "page_1",
                    "image_hash_1",
                    "https://example.com/landing",
                    "Texto principal",
                    "Titulo",
                    null,
                    null),
                CancellationToken.None));

        Assert.Equal("100", ex.Code);
        Assert.Equal("1885316", ex.ErrorSubcode);
        Assert.Equal("Creative invalido", ex.ErrorUserTitle);
        Assert.Equal("Revise os campos.", ex.ErrorUserMessage);
        Assert.Equal("trace-creative", ex.FbTraceId);
        Assert.DoesNotContain("token-secreto", ex.Message);
    }

    [Fact]
    public async Task DeleteAdCreativeAsync_SuccessTrueConclui()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{ "success": true }"""));
        var client = Client(handler);

        await client.DeleteAdCreativeAsync(Config(), "token-secreto", "creative_1", CancellationToken.None);

        Assert.Equal(HttpMethod.Delete, handler.LastMethod);
        Assert.Contains("/v23.0/creative_1", handler.LastRequestUri);
        Assert.DoesNotContain("access_token", handler.LastRequestUri);
    }

    [Fact]
    public async Task CreateAdAsync_EnviaSomentePayloadMinimoComPaused()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{ "id": "ad_1" }"""));
        var client = Client(handler);

        await client.CreateAdAsync(
            Config(),
            "token-secreto",
            "act_1668410610924666",
            new MetaAdsAdCreatePayload("LeadEngine - Ad", "adset_1", "creative_1", "ACTIVE"),
            CancellationToken.None);

        var form = ParseForm(handler.LastRequestBody);
        Assert.Equal(HttpMethod.Post, handler.LastMethod);
        Assert.Contains("/v23.0/act_1668410610924666/ads", handler.LastRequestUri);
        Assert.Equal(["adset_id", "creative", "name", "status"], form.Keys.Order(StringComparer.Ordinal).ToArray());
        Assert.Equal("LeadEngine - Ad", form["name"]);
        Assert.Equal("adset_1", form["adset_id"]);
        Assert.Equal("""{"creative_id":"creative_1"}""", form["creative"]);
        Assert.Equal("PAUSED", form["status"]);
        Assert.DoesNotContain("ACTIVE", Decode(handler.LastRequestBody));
        Assert.DoesNotContain("access_token", handler.LastRequestUri);
    }

    [Fact]
    public async Task CreateAdAsync_NaoEnviaTrackingPixelCapiInstagramOrcamentoBidPlacementOuPromotedObject()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{ "id": "ad_1" }"""));
        var client = Client(handler);

        await client.CreateAdAsync(
            Config(),
            "token-secreto",
            "act_1668410610924666",
            new MetaAdsAdCreatePayload("LeadEngine - Ad", "adset_1", "creative_1", "ACTIVE"),
            CancellationToken.None);

        var body = Decode(handler.LastRequestBody);
        Assert.DoesNotContain("tracking_specs", body);
        Assert.DoesNotContain("pixel", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("capi", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("instagram", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("daily_budget", body);
        Assert.DoesNotContain("lifetime_budget", body);
        Assert.DoesNotContain("bid_", body);
        Assert.DoesNotContain("placement", body);
        Assert.DoesNotContain("promoted_object", body);
    }

    [Fact]
    public async Task CreateAdAsync_FluxoAntigoPreservado()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{ "id": "ad_1" }"""));
        var client = Client(handler);

        await client.CreateAdAsync(
            Config(),
            "token-secreto",
            "act_1668410610924666",
            new MetaAdsAdCreatePayload("LeadEngine - Publicacao - Ad", "adset_1", "creative_1", MetaAdsConstants.StatusPaused),
            CancellationToken.None);

        var form = ParseForm(handler.LastRequestBody);
        Assert.Equal(["adset_id", "creative", "name", "status"], form.Keys.Order(StringComparer.Ordinal).ToArray());
        Assert.Equal("LeadEngine - Publicacao - Ad", form["name"]);
        Assert.Equal("adset_1", form["adset_id"]);
        Assert.Equal("""{"creative_id":"creative_1"}""", form["creative"]);
        Assert.Equal("PAUSED", form["status"]);
    }

    [Fact]
    public async Task CreateAdAsync_ErroMetaConvertidoESanitizado()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
        {
          "error": {
            "message": "Invalid OAuth access_token=token-secreto",
            "type": "OAuthException",
            "code": 100,
            "error_subcode": 1885183,
            "error_user_title": "Ad invalido",
            "error_user_msg": "Revise o Ad.",
            "fbtrace_id": "trace-ad"
          }
        }
        """, HttpStatusCode.BadRequest));
        var client = Client(handler);

        var ex = await Assert.ThrowsAsync<MetaAdsGraphApiException>(() =>
            client.CreateAdAsync(
                Config(),
                "token-secreto",
                "act_1668410610924666",
                new MetaAdsAdCreatePayload("LeadEngine - Ad", "adset_1", "creative_1", MetaAdsConstants.StatusPaused),
                CancellationToken.None));

        Assert.Equal("100", ex.Code);
        Assert.Equal("1885183", ex.ErrorSubcode);
        Assert.Equal("Ad invalido", ex.ErrorUserTitle);
        Assert.Equal("Revise o Ad.", ex.ErrorUserMessage);
        Assert.Equal("trace-ad", ex.FbTraceId);
        Assert.DoesNotContain("token-secreto", ex.Message);
    }

    [Fact]
    public async Task DeleteAdAsync_SuccessTrueConclui()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{ "success": true }"""));
        var client = Client(handler);

        await client.DeleteAdAsync(Config(), "token-secreto", "ad_1", CancellationToken.None);

        Assert.Equal(HttpMethod.Delete, handler.LastMethod);
        Assert.Contains("/v23.0/ad_1", handler.LastRequestUri);
        Assert.DoesNotContain("access_token", handler.LastRequestUri);
    }

    [Fact]
    public async Task DiagnosticsService_CreateAdSet_CampaignIdVazioFalha()
    {
        var service = new MetaAdsDiagnosticsService(new FakeResolver(), new ThrowingGraphClient());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAdSetAsync(ValidAdSetRequest() with { CampaignId = " " }, CancellationToken.None));
    }

    [Fact]
    public async Task DiagnosticsService_CreateAdSet_BudgetInvalidoFalha()
    {
        var service = new MetaAdsDiagnosticsService(new FakeResolver(), new ThrowingGraphClient());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAdSetAsync(ValidAdSetRequest() with { DailyBudget = 0 }, CancellationToken.None));
    }

    [Fact]
    public async Task DiagnosticsService_CreateAdSet_LocalizacaoAusenteFalha()
    {
        var service = new MetaAdsDiagnosticsService(new FakeResolver(), new ThrowingGraphClient());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAdSetAsync(ValidAdSetRequest() with { Targeting = new MetaTargetingRequest([], [], [], null, null, null) }, CancellationToken.None));
    }

    [Fact]
    public async Task DiagnosticsService_CreateAdSet_EnviaLowestCostWithoutCapSemBidAmount()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{ "id": "adset_1" }"""));
        var service = new MetaAdsDiagnosticsService(new FakeResolver(), Client(handler));

        await service.CreateAdSetAsync(ValidAdSetRequest(), CancellationToken.None);

        var body = Decode(handler.LastRequestBody);
        Assert.Contains("name=LeadEngine - AdSet", body);
        Assert.Contains("campaign_id=campaign_1", body);
        Assert.Contains("daily_budget=2000", body);
        Assert.Contains("billing_event=IMPRESSIONS", body);
        Assert.Contains("optimization_goal=LEAD_GENERATION", body);
        Assert.Contains("bid_strategy=LOWEST_COST_WITHOUT_CAP", body);
        Assert.Contains("\"geo_locations\":{\"countries\":[\"BR\"]}", body);
        Assert.Contains("\"targeting_automation\":{\"advantage_audience\":0}", body);
        Assert.Contains("status=PAUSED", body);
        Assert.DoesNotContain("bid_amount", body);
        Assert.DoesNotContain("bid_constraints", body);
    }

    [Fact]
    public async Task DiagnosticsService_CreateCreative_RequestValidoEnviaPayload()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{ "id": "creative_1" }"""));
        var service = new MetaAdsDiagnosticsService(new FakeResolver(), Client(handler));

        await service.CreateCreativeAsync(ValidCreativeRequest(), CancellationToken.None);

        var body = Decode(handler.LastRequestBody);
        Assert.Contains("name=LeadEngine - Creative", body);
        Assert.Contains("\"page_id\":\"page_1\"", body);
        Assert.Contains("\"image_hash\":\"image_hash_1\"", body);
        Assert.Contains("\"link\":\"https://example.com/landing\"", body);
        Assert.Contains("\"message\":\"Texto principal\"", body);
        Assert.Contains("\"name\":\"Titulo\"", body);
        Assert.DoesNotContain("instagram_actor_id", body);
        Assert.DoesNotContain("call_to_action", body);
        Assert.DoesNotContain("description", body);
    }

    [Fact]
    public async Task DiagnosticsService_CreateCreative_UrlInvalidaFalha()
    {
        var service = new MetaAdsDiagnosticsService(new FakeResolver(), new ThrowingGraphClient());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateCreativeAsync(ValidCreativeRequest() with { LinkUrl = "url-invalida" }, CancellationToken.None));
    }

    [Fact]
    public async Task DiagnosticsService_CreateCreative_PageIdVazioFalha()
    {
        var service = new MetaAdsDiagnosticsService(new FakeResolver(), new ThrowingGraphClient());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateCreativeAsync(ValidCreativeRequest() with { PageId = " " }, CancellationToken.None));
    }

    [Fact]
    public async Task DiagnosticsService_CreateCreative_ImageHashVazioFalha()
    {
        var service = new MetaAdsDiagnosticsService(new FakeResolver(), new ThrowingGraphClient());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateCreativeAsync(ValidCreativeRequest() with { ImageHash = " " }, CancellationToken.None));
    }

    [Fact]
    public async Task DiagnosticsService_CreateCreative_CtaDiferenteDeLearnMoreFalha()
    {
        var service = new MetaAdsDiagnosticsService(new FakeResolver(), new ThrowingGraphClient());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateCreativeAsync(ValidCreativeRequest() with { CallToActionType = "SIGN_UP" }, CancellationToken.None));
    }

    [Fact]
    public async Task DiagnosticsService_CreateCreative_LearnMoreSerializado()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{ "id": "creative_1" }"""));
        var service = new MetaAdsDiagnosticsService(new FakeResolver(), Client(handler));

        await service.CreateCreativeAsync(ValidCreativeRequest() with { Description = "Descricao", CallToActionType = "LEARN_MORE" }, CancellationToken.None);

        var body = Decode(handler.LastRequestBody);
        Assert.Contains("\"description\":\"Descricao\"", body);
        Assert.Contains("\"call_to_action\":{\"type\":\"LEARN_MORE\",\"value\":{\"link\":\"https://example.com/landing\"}}", body);
    }

    [Fact]
    public async Task DiagnosticsService_CreateAd_RequestValidoEnviaSomentePayloadMinimoPaused()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{ "id": "ad_1" }"""));
        var service = new MetaAdsDiagnosticsService(new FakeResolver(), Client(handler));

        var result = await service.CreateAdAsync(ValidAdRequest(), CancellationToken.None);

        var form = ParseForm(handler.LastRequestBody);
        Assert.Equal("ad_1", result.Id);
        Assert.Equal(["adset_id", "creative", "name", "status"], form.Keys.Order(StringComparer.Ordinal).ToArray());
        Assert.Equal("LeadEngine - Ad", form["name"]);
        Assert.Equal("adset_1", form["adset_id"]);
        Assert.Equal("""{"creative_id":"creative_1"}""", form["creative"]);
        Assert.Equal("PAUSED", form["status"]);
        Assert.DoesNotContain("ACTIVE", Decode(handler.LastRequestBody));
    }

    [Fact]
    public async Task DiagnosticsService_CreateAd_NameVazioFalha()
    {
        var service = new MetaAdsDiagnosticsService(new FakeResolver(), new ThrowingGraphClient());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAdAsync(ValidAdRequest() with { Name = " " }, CancellationToken.None));
    }

    [Fact]
    public async Task DiagnosticsService_CreateAd_AdSetIdVazioFalha()
    {
        var service = new MetaAdsDiagnosticsService(new FakeResolver(), new ThrowingGraphClient());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAdAsync(ValidAdRequest() with { AdSetId = " " }, CancellationToken.None));
    }

    [Fact]
    public async Task DiagnosticsService_CreateAd_CreativeIdVazioFalha()
    {
        var service = new MetaAdsDiagnosticsService(new FakeResolver(), new ThrowingGraphClient());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAdAsync(ValidAdRequest() with { CreativeId = " " }, CancellationToken.None));
    }

    [Fact]
    public async Task DiagnosticsService_DeleteAd_RetornaSuccessTrue()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{ "success": true }"""));
        var service = new MetaAdsDiagnosticsService(new FakeResolver(), Client(handler));

        var result = await service.DeleteAdAsync("ad_1", CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Delete, handler.LastMethod);
        Assert.Contains("/v23.0/ad_1", handler.LastRequestUri);
    }

    private static MetaAdsGraphClient Client(StubHttpMessageHandler handler)
    {
        return new MetaAdsGraphClient(new StubHttpClientFactory(new HttpClient(handler)), NullLogger<MetaAdsGraphClient>.Instance);
    }

    private static MetaAdsConfiguration Config()
    {
        return new MetaAdsConfiguration(
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            "https://graph.facebook.com",
            "v23.0",
            string.Empty);
    }

    private static HttpResponseMessage JsonResponse(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static CreateMetaAdSetRequest ValidAdSetRequest()
    {
        return new CreateMetaAdSetRequest(
            "campaign_1",
            "LeadEngine - AdSet",
            2000,
            MetaAdsConstants.BillingEventImpressions,
            MetaAdsConstants.OptimizationGoalLeadGeneration,
            null,
            null,
            new MetaTargetingRequest(["BR"], [], [], null, null, null));
    }

    private static CreateMetaCreativeRequest ValidCreativeRequest()
    {
        return new CreateMetaCreativeRequest(
            "LeadEngine - Creative",
            "page_1",
            "image_hash_1",
            "Texto principal",
            "https://example.com/landing",
            "Titulo");
    }

    private static CreateMetaAdRequest ValidAdRequest()
    {
        return new CreateMetaAdRequest(
            "LeadEngine - Ad",
            "adset_1",
            "creative_1");
    }

    private static string Decode(string body)
    {
        return WebUtility.UrlDecode(body.Replace("+", "%20")) ?? string.Empty;
    }

    private static Dictionary<string, string> ParseForm(string body)
    {
        return body.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Split('=', 2))
            .ToDictionary(
                x => WebUtility.UrlDecode(x[0]) ?? string.Empty,
                x => x.Length > 1 ? Decode(x[1]) : string.Empty,
                StringComparer.Ordinal);
    }

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public string LastRequestUri { get; private set; } = string.Empty;
        public string LastRequestBody { get; private set; } = string.Empty;
        public HttpMethod? LastMethod { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastMethod = request.Method;
            LastRequestUri = request.RequestUri?.ToString() ?? string.Empty;
            LastRequestBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            return responder(request);
        }
    }

    private sealed class FakeResolver : IConfigurationResolver
    {
        public Task<ResolvedConfigurationValue> ResolveAsync(CategoriaConfiguracao categoria, string chave, CancellationToken cancellationToken)
        {
            var value = chave switch
            {
                "AccessToken" => "token-secreto",
                "AdAccountId" => "act_1668410610924666",
                "GraphApiBaseUrl" => "https://graph.facebook.com",
                "GraphApiVersion" => "v23.0",
                _ => string.Empty
            };
            return Task.FromResult(new ResolvedConfigurationValue(value, true, OrigemConfiguracao.VariavelAmbiente, chave is "AccessToken"));
        }

        public Task InvalidateAsync(CategoriaConfiguracao categoria, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class ThrowingGraphClient : IMetaAdsGraphClient
    {
        public Task<MetaAdAccountDto> GetAdAccountAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<MetaCampaignDto>> GetCampaignsAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<MetaAdSetDto>> GetAdSetsAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<MetaAdDto>> GetAdsAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<MetaCreativeDto>> GetAdCreativesAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<MetaAdsBusinessResponse>> ListBusinessesAsync(MetaAdsConfiguration config, string accessToken, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<MetaAdsAdAccountResponse>> ListAdAccountsAsync(MetaAdsConfiguration config, string accessToken, string businessId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<MetaAdsPageResponse>> ListPagesAsync(MetaAdsConfiguration config, string accessToken, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MetaAdsPageResponse?> GetPageAsync(MetaAdsConfiguration config, string accessToken, string pageId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<MetaAdsPixelResponse>> ListPixelsAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MetaAdsPermissionStatusResponse> GetPermissionsAsync(MetaAdsConfiguration config, string accessToken, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<MetaAdsLocationResponse>> SearchTargetingLocationsAsync(MetaAdsConfiguration config, string accessToken, string query, string countryCode, int limit, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<string> UploadAdImageAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, string fileName, string contentType, byte[] content, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> ResourceExistsAsync(MetaAdsConfiguration config, string accessToken, string resourceId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MetaAdsCreateResult> CreateCampaignAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, MetaAdsCampaignCreatePayload payload, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteCampaignAsync(MetaAdsConfiguration config, string accessToken, string campaignId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MetaAdsCreateResult> CreateAdSetAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, MetaAdsAdSetCreatePayload payload, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteAdSetAsync(MetaAdsConfiguration config, string accessToken, string adSetId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MetaAdsCreateResult> CreateAdCreativeAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, MetaAdsCreativeCreatePayload payload, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MetaAdsCreateResult> CreateDiagnosticAdCreativeAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, MetaAdsDiagnosticCreativeCreatePayload payload, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteAdCreativeAsync(MetaAdsConfiguration config, string accessToken, string creativeId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MetaAdsCreateResult> CreateAdAsync(MetaAdsConfiguration config, string accessToken, string adAccountId, MetaAdsAdCreatePayload payload, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteAdAsync(MetaAdsConfiguration config, string accessToken, string adId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
