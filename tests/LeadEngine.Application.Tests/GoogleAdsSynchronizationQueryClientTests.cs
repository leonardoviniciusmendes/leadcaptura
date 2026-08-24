using System.Net;
using System.Text;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Enums;
using LeadEngine.Infrastructure.GoogleAds;
using Microsoft.Extensions.Logging.Abstractions;

namespace LeadEngine.Application.Tests;

public sealed class GoogleAdsSynchronizationQueryClientTests
{
    [Fact]
    public async Task SetResourceStatusesAsync_EnviaSomenteRecursosAtivaveisEmOrdemSegura()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{ "mutateOperationResponses": [] }"""));
        var client = new GoogleAdsSynchronizationQueryClient(
            new GoogleAdsGaqlClient(new StubHttpClientFactory(new HttpClient(handler)), new Resolver(), new GoogleAdsExceptionFormatter(NullLogger<GoogleAdsExceptionFormatter>.Instance)),
            new StubHttpClientFactory(new HttpClient(handler)),
            new Resolver(loginCustomerId: "194-845-9907"),
            new GoogleAdsExceptionFormatter(NullLogger<GoogleAdsExceptionFormatter>.Instance));
        var resources = new GoogleAdsPublishedResourceDto[]
        {
            new("Campaign", "customers/9890172254/campaigns/1", "1", "Campaign", "PAUSED"),
            new("Budget", "customers/9890172254/campaignBudgets/1", "1", "Budget", "PAUSED"),
            new("CampaignCriterion", "customers/9890172254/campaignCriteria/1", "1", "Geo", "PAUSED"),
            new("AdGroup", "customers/9890172254/adGroups/2", "2", "AdGroup", "PAUSED"),
            new("Keyword", "customers/9890172254/adGroupCriteria/3", "3", "kw", "PAUSED"),
            new("ResponsiveSearchAd", "customers/9890172254/adGroupAds/4", "4", "RSA", "PAUSED")
        };

        await client.SetResourceStatusesAsync("9890172254", "access-token", "developer-token", resources, "ENABLED", CancellationToken.None);

        Assert.EndsWith("/customers/9890172254/googleAds:mutate", handler.LastRequest!.RequestUri!.ToString(), StringComparison.Ordinal);
        Assert.Equal("Bearer access-token", handler.LastHeaders["Authorization"]);
        Assert.Equal("developer-token", handler.LastHeaders["developer-token"]);
        Assert.Equal("1948459907", handler.LastHeaders["login-customer-id"]);
        Assert.Contains("\"adGroupOperation\"", handler.LastRequestBody);
        Assert.Contains("\"adGroupCriterionOperation\"", handler.LastRequestBody);
        Assert.Contains("\"adGroupAdOperation\"", handler.LastRequestBody);
        Assert.Contains("\"campaignOperation\"", handler.LastRequestBody);
        Assert.DoesNotContain("campaignBudgetOperation", handler.LastRequestBody);
        Assert.DoesNotContain("campaignCriteria/1", handler.LastRequestBody);
        Assert.Equal(4, Count(handler.LastRequestBody, "\"status\":\"ENABLED\""));
        Assert.True(handler.LastRequestBody.IndexOf("adGroupOperation", StringComparison.Ordinal) < handler.LastRequestBody.IndexOf("campaignOperation", StringComparison.Ordinal));
        Assert.DoesNotContain("access-token", handler.LastRequestBody);
        Assert.DoesNotContain("developer-token", handler.LastRequestBody);
    }

    private static int Count(string value, string pattern)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += pattern.Length;
        }

        return count;
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
        public HttpRequestMessage? LastRequest { get; private set; }
        public string LastRequestBody { get; private set; } = string.Empty;
        public Dictionary<string, string> LastHeaders { get; } = new(StringComparer.OrdinalIgnoreCase);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastRequestBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            LastHeaders.Clear();
            foreach (var header in request.Headers)
            {
                LastHeaders[header.Key] = string.Join(",", header.Value);
            }

            return responder(request);
        }
    }

    private sealed class Resolver(string loginCustomerId = "") : IConfigurationResolver
    {
        public Task<ResolvedConfigurationValue> ResolveAsync(CategoriaConfiguracao categoria, string chave, CancellationToken cancellationToken)
        {
            var value = chave switch
            {
                "ApiBaseUrl" => "https://googleads.googleapis.com/v22",
                "LoginCustomerId" => loginCustomerId,
                _ => string.Empty
            };
            return Task.FromResult(new ResolvedConfigurationValue(value, !string.IsNullOrWhiteSpace(value), OrigemConfiguracao.Banco, false));
        }

        public Task InvalidateAsync(CategoriaConfiguracao categoria, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
