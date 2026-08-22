using System.Net;
using System.Text;
using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Services;
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
        { "data": [{ "id": "111", "name": "Campanha", "status": "PAUSED", "effective_status": "PAUSED" }] }
        """));
        var client = Client(handler);

        var result = await client.GetCampaignsAsync(Config(), "token-secreto", "1668410610924666", CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal("111", item.Id);
        Assert.Equal("Campanha", item.Name);
        Assert.Equal("PAUSED", item.Status);
        Assert.Equal("PAUSED", item.EffectiveStatus);
        Assert.Contains("/v23.0/act_1668410610924666/campaigns", handler.LastRequestUri);
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
}
