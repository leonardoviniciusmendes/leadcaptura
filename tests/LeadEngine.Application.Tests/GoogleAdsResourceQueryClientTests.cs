using System.Net;
using System.Text;
using System.Text.Json;
using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Enums;
using LeadEngine.Infrastructure.GoogleAds;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LeadEngine.Application.Tests;

public sealed class GoogleAdsResourceQueryClientTests
{
    [Fact]
    public async Task CheckResourcesAsync_ConsultaRecursosEmLotePorTipo()
    {
        var handler = new StubHttpMessageHandler(request => JsonResponse(ResponseFor(request)));
        var client = Client(handler);
        var resources = Resources();

        var result = await client.CheckResourcesAsync("9890172254", "access-token", "developer-token", resources, CancellationToken.None);

        Assert.Equal(resources.Count, result.Count);
        Assert.All(result, x => Assert.True(x.Encontrado));
        Assert.Equal(6, handler.RequestBodies.Count);
        Assert.Contains(handler.RequestBodies, x => x.Contains("FROM campaign_criterion", StringComparison.Ordinal) && x.Contains("campaignCriteria/3", StringComparison.Ordinal) && x.Contains("campaignCriteria/4", StringComparison.Ordinal));
        Assert.Contains(handler.RequestBodies, x => x.Contains("FROM ad_group_criterion", StringComparison.Ordinal) && x.Contains("adGroupCriteria/6", StringComparison.Ordinal) && x.Contains("adGroupCriteria/7", StringComparison.Ordinal));
        Assert.All(handler.Requests, request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.EndsWith("/customers/9890172254/googleAds:search", request.RequestUri!.ToString(), StringComparison.Ordinal);
        });
        Assert.DoesNotContain(handler.RequestBodies, x => x.Contains("mutate", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(handler.RequestBodies, x => x.Contains("ENABLED", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CheckResourcesAsync_EnviaLoginCustomerIdSomenteQuandoConfigurado()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{ "results": [] }"""));
        var client = Client(handler, "194-845-9907");

        await client.CheckResourcesAsync("9890172254", "access-token", "developer-token", [Resources()[1]], CancellationToken.None);

        Assert.Equal("1948459907", handler.LastHeaders["login-customer-id"]);

        handler = new StubHttpMessageHandler(_ => JsonResponse("""{ "results": [] }"""));
        client = Client(handler, "");

        await client.CheckResourcesAsync("9890172254", "access-token", "developer-token", [Resources()[1]], CancellationToken.None);

        Assert.False(handler.LastHeaders.ContainsKey("login-customer-id"));
    }

    [Fact]
    public async Task CheckResourcesAsync_ErroGoogleAdsPermaneceSanitizado()
    {
        var logger = new CaptureLogger<GoogleAdsExceptionFormatter>();
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
        {
          "error": {
            "code": 403,
            "message": "developer_token=developer-secret Authorization: Bearer access-secret client_secret=client-secret refresh_token=refresh-secret",
            "status": "PERMISSION_DENIED"
          }
        }
        """, HttpStatusCode.Forbidden));
        var client = Client(handler, formatter: new GoogleAdsExceptionFormatter(logger));

        var ex = await Assert.ThrowsAsync<GoogleAdsDiagnosticException>(() => client.CheckResourcesAsync("9890172254", "access-secret", "developer-secret", [Resources()[1]], CancellationToken.None));
        var diagnostic = JsonSerializer.Serialize(ex.Diagnostic);

        Assert.DoesNotContain("access-secret", diagnostic);
        Assert.DoesNotContain("developer-secret", diagnostic);
        Assert.DoesNotContain("client-secret", diagnostic);
        Assert.DoesNotContain("refresh-secret", diagnostic);
        Assert.DoesNotContain("access-secret", logger.LastMessage);
        Assert.DoesNotContain("developer-secret", logger.LastMessage);
        Assert.DoesNotContain("client-secret", logger.LastMessage);
        Assert.DoesNotContain("refresh-secret", logger.LastMessage);
    }

    [Fact]
    public async Task CheckResourcesAsync_PropagaCancelamentoSemCredenciais()
    {
        var handler = new StubHttpMessageHandler((_, cancellationToken) => Task.FromCanceled<HttpResponseMessage>(cancellationToken));
        var client = Client(handler);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.CheckResourcesAsync("9890172254", "access-secret", "developer-secret", [Resources()[1]], cts.Token));

        Assert.DoesNotContain("access-secret", ex.ToString());
        Assert.DoesNotContain("developer-secret", ex.ToString());
    }

    private static GoogleAdsResourceQueryClient Client(StubHttpMessageHandler handler, string loginCustomerId = "", GoogleAdsExceptionFormatter? formatter = null)
    {
        return new GoogleAdsResourceQueryClient(
            new StubHttpClientFactory(new HttpClient(handler)),
            new Resolver(loginCustomerId),
            formatter ?? new GoogleAdsExceptionFormatter(NullLogger<GoogleAdsExceptionFormatter>.Instance));
    }

    private static IReadOnlyList<GoogleAdsPublishedResourceDto> Resources() =>
    [
        new("Budget", "customers/9890172254/campaignBudgets/1", "1", "Budget", "PAUSED"),
        new("Campaign", "customers/9890172254/campaigns/2", "2", "Campaign", "PAUSED"),
        new("CampaignCriterion", "customers/9890172254/campaignCriteria/3", "3", null, "PAUSED"),
        new("NegativeKeyword", "customers/9890172254/campaignCriteria/4", "4", null, "PAUSED"),
        new("AdGroup", "customers/9890172254/adGroups/5", "5", "AdGroup", "PAUSED"),
        new("Keyword", "customers/9890172254/adGroupCriteria/6", "6", "kw 1", "PAUSED"),
        new("Keyword", "customers/9890172254/adGroupCriteria/7", "7", "kw 2", "PAUSED"),
        new("ResponsiveSearchAd", "customers/9890172254/adGroupAds/8", "8", "Ad", "PAUSED")
    ];

    private static string ResponseFor(HttpRequestMessage request)
    {
        var body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
        return body switch
        {
            var x when x.Contains("FROM campaign_budget", StringComparison.Ordinal) => """{ "results": [{ "campaignBudget": { "resourceName": "customers/9890172254/campaignBudgets/1" } }] }""",
            var x when x.Contains("FROM campaign WHERE", StringComparison.Ordinal) => """{ "results": [{ "campaign": { "resourceName": "customers/9890172254/campaigns/2" } }] }""",
            var x when x.Contains("FROM campaign_criterion", StringComparison.Ordinal) => """{ "results": [{ "campaignCriterion": { "resourceName": "customers/9890172254/campaignCriteria/3" } }, { "campaignCriterion": { "resourceName": "customers/9890172254/campaignCriteria/4" } }] }""",
            var x when x.Contains("FROM ad_group WHERE", StringComparison.Ordinal) => """{ "results": [{ "adGroup": { "resourceName": "customers/9890172254/adGroups/5" } }] }""",
            var x when x.Contains("FROM ad_group_criterion", StringComparison.Ordinal) => """{ "results": [{ "adGroupCriterion": { "resourceName": "customers/9890172254/adGroupCriteria/6" } }, { "adGroupCriterion": { "resourceName": "customers/9890172254/adGroupCriteria/7" } }] }""",
            var x when x.Contains("FROM ad_group_ad", StringComparison.Ordinal) => """{ "results": [{ "adGroupAd": { "resourceName": "customers/9890172254/adGroupAds/8" } }] }""",
            _ => """{ "results": [] }"""
        };
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

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            : this((request, _) => Task.FromResult(responder(request)))
        {
        }

        public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
        {
            this.responder = responder;
        }

        public List<HttpRequestMessage> Requests { get; } = [];
        public List<string> RequestBodies { get; } = [];
        public Dictionary<string, string> LastHeaders { get; } = new(StringComparer.OrdinalIgnoreCase);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastHeaders.Clear();
            foreach (var header in request.Headers)
            {
                LastHeaders[header.Key] = string.Join(",", header.Value);
            }

            RequestBodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
            Requests.Add(request);
            return await responder(request, cancellationToken);
        }
    }

    private sealed class Resolver(string loginCustomerId) : IConfigurationResolver
    {
        public Task<ResolvedConfigurationValue> ResolveAsync(CategoriaConfiguracao categoria, string chave, CancellationToken cancellationToken)
        {
            var value = chave switch
            {
                "ApiBaseUrl" => "https://googleads.googleapis.com/v22",
                "ApiTimeoutSeconds" => "60",
                "LoginCustomerId" => loginCustomerId,
                _ => string.Empty
            };
            return Task.FromResult(new ResolvedConfigurationValue(value, !string.IsNullOrWhiteSpace(value), OrigemConfiguracao.Banco, chave.Contains("Token", StringComparison.OrdinalIgnoreCase)));
        }

        public Task InvalidateAsync(CategoriaConfiguracao categoria, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class CaptureLogger<T> : ILogger<T>
    {
        public string LastMessage { get; private set; } = string.Empty;
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LastMessage = formatter(state, exception);
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }
}
