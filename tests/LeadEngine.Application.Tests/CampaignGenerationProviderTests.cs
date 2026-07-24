using System.Net;
using System.Text;
using LeadEngine.Application.Common;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Infrastructure.CampaignGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LeadEngine.Application.Tests;

public sealed class CampaignGenerationProviderTests
{
    [Fact]
    public async Task Provider_SelecionaFake()
    {
        var service = Configured("Fake", OpenRouter("{}"));

        var result = await service.GenerateAsync(CampanhaServiceTests.BriefingPadrao(), CancellationToken.None);

        Assert.Equal("Fake", result.Provider);
    }

    [Fact]
    public async Task Provider_SelecionaOpenRouter()
    {
        var service = Configured("OpenRouter", OpenRouter(OpenRouterResponse(CampaignGenerationParserTests.JsonValido())));

        var result = await service.GenerateAsync(CampanhaServiceTests.BriefingPadrao(), CancellationToken.None);

        Assert.Equal("OpenRouter", result.Provider);
    }

    [Fact]
    public async Task OpenRouter_SemApiKeyFalha()
    {
        var service = OpenRouter(OpenRouterResponse(CampaignGenerationParserTests.JsonValido()), apiKey: "");

        await Assert.ThrowsAsync<CampaignGenerationException>(() => service.GenerateAsync(CampanhaServiceTests.BriefingPadrao(), CancellationToken.None));
    }

    [Fact]
    public async Task Provider_FallbackDesativadoFalha()
    {
        var service = Configured("OpenRouter", OpenRouter("erro", HttpStatusCode.InternalServerError), fallbackToFake: false);

        await Assert.ThrowsAsync<CampaignGenerationException>(() => service.GenerateAsync(CampanhaServiceTests.BriefingPadrao(), CancellationToken.None));
    }

    [Fact]
    public async Task Provider_FallbackAtivadoUsaFake()
    {
        var service = Configured("OpenRouter", OpenRouter("erro", HttpStatusCode.InternalServerError), fallbackToFake: true);

        var result = await service.GenerateAsync(CampanhaServiceTests.BriefingPadrao(), CancellationToken.None);

        Assert.Equal("Fake", result.Provider);
    }

    private static ConfiguredCampaignGenerationService Configured(string provider, OpenRouterCampaignGenerationService openRouter, bool fallbackToFake = false)
    {
        return new ConfiguredCampaignGenerationService(
            Options.Create(new CampaignGenerationOptions { Provider = provider, FallbackToFake = fallbackToFake }),
            new FakeCampaignGenerationService(),
            openRouter,
            NullLogger<ConfiguredCampaignGenerationService>.Instance);
    }

    private static OpenRouterCampaignGenerationService OpenRouter(string response, HttpStatusCode statusCode = HttpStatusCode.OK, string apiKey = "key")
    {
        var client = new HttpClient(new StubHttpMessageHandler(statusCode, response))
        {
            BaseAddress = new Uri("https://openrouter.ai/api/v1/")
        };

        return new OpenRouterCampaignGenerationService(
            new StubHttpClientFactory(client),
            Options.Create(new OpenRouterOptions
            {
                ApiKey = apiKey,
                Model = "test-model",
                MaxRetries = 0,
                TimeoutSeconds = 10
            }),
            new CampaignPromptBuilder(),
            new CampaignGenerationResponseParser(),
            NullLogger<OpenRouterCampaignGenerationService>.Instance);
    }

    private static string OpenRouterResponse(string content)
    {
        return $$"""
        {
          "choices": [
            {
              "message": {
                "content": {{System.Text.Json.JsonSerializer.Serialize(content)}}
              }
            }
          ]
        }
        """;
    }

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHttpMessageHandler(HttpStatusCode statusCode, string response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(response, Encoding.UTF8, "application/json")
            });
        }
    }
}
