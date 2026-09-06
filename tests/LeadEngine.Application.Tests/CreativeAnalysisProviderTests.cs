using System.Net;
using System.Text;
using System.Text.Json;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Enums;
using LeadEngine.Infrastructure.CampaignGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeadEngine.Application.Tests;

[Collection("CreativeAnalysisEnvironment")]
public sealed class CreativeAnalysisProviderTests : IDisposable
{
    private readonly Dictionary<string, string?> environment = new()
    {
        ["OPENROUTER_API_KEY"] = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY"),
        ["OPENROUTER_BASE_URL"] = Environment.GetEnvironmentVariable("OPENROUTER_BASE_URL"),
        ["CREATIVE_ANALYSIS_MODEL"] = Environment.GetEnvironmentVariable("CREATIVE_ANALYSIS_MODEL"),
        ["CREATIVE_ANALYSIS_COST_TIER"] = Environment.GetEnvironmentVariable("CREATIVE_ANALYSIS_COST_TIER")
    };

    public CreativeAnalysisProviderTests()
    {
        foreach (var key in environment.Keys)
        {
            Environment.SetEnvironmentVariable(key, null);
        }
    }

    [Fact]
    public async Task ModelAusente_UsaOpenRouterAuto()
    {
        var handler = new CaptureHttpMessageHandler(_ => JsonResponse(OpenRouterResponse("openai/gpt-4o-mini", ValidAnalysisJson())));
        var provider = Provider(handler, new CreativeAnalysisOptions { Provider = "OpenRouter", Model = "", CostTier = "low" });

        await provider.AnalyzeAsync(Request(), CancellationToken.None);

        using var doc = JsonDocument.Parse(handler.LastBody);
        Assert.Equal("openrouter/auto", doc.RootElement.GetProperty("model").GetString());
    }

    [Fact]
    public async Task CostTierAusente_UsaLow()
    {
        var handler = new CaptureHttpMessageHandler(_ => JsonResponse(OpenRouterResponse("openai/gpt-4o-mini", ValidAnalysisJson())));
        var provider = Provider(handler, new CreativeAnalysisOptions { Provider = "OpenRouter", Model = "openrouter/auto", CostTier = "" });

        await provider.AnalyzeAsync(Request(), CancellationToken.None);

        using var doc = JsonDocument.Parse(handler.LastBody);
        Assert.Equal("low", doc.RootElement.GetProperty("cost_tier").GetString());
    }

    [Fact]
    public async Task CostTierInvalido_GeraErroControlado()
    {
        var handler = new CaptureHttpMessageHandler(_ => JsonResponse(OpenRouterResponse("openai/gpt-4o-mini", ValidAnalysisJson())));
        var provider = Provider(handler, new CreativeAnalysisOptions { Provider = "OpenRouter", Model = "openrouter/auto", CostTier = "cheap" });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.AnalyzeAsync(Request(), CancellationToken.None));

        Assert.Contains("CostTier invalido", ex.Message);
    }

    [Fact]
    public async Task RoutedModelRetornado_EhUsadoNoResultado()
    {
        var handler = new CaptureHttpMessageHandler(_ => JsonResponse(OpenRouterResponse("google/gemini-2.0-flash", ValidAnalysisJson())));
        var provider = Provider(handler, new CreativeAnalysisOptions { Provider = "OpenRouter", Model = "openrouter/auto", CostTier = "medium" });

        var result = await provider.AnalyzeAsync(Request(), CancellationToken.None);

        Assert.Equal("OpenRouter", result.Provider);
        Assert.Equal("google/gemini-2.0-flash", result.Model);
    }

    [Fact]
    public async Task FakeContinuaFuncionandoQuandoSelecionadoExplicitamente()
    {
        var configured = new ConfiguredCreativeAssetAnalysisProvider(
            Options.Create(new CreativeAnalysisOptions { Provider = "Fake", Model = "openrouter/auto", CostTier = "low", FallbackToFake = false }),
            new FakeCreativeAssetAnalysisProvider(),
            Provider(new CaptureHttpMessageHandler(_ => throw new InvalidOperationException("OpenRouter nao deveria ser chamado."))),
            new CaptureLogger<ConfiguredCreativeAssetAnalysisProvider>());

        var result = await configured.AnalyzeAsync(Request(), CancellationToken.None);

        Assert.Equal("Fake", result.Provider);
        Assert.Equal("fake-vision-v1", result.Model);
    }

    [Fact]
    public async Task LogsNaoIncluemApiKey()
    {
        var logger = new CaptureLogger<OpenRouterCreativeAssetAnalysisProvider>();
        var handler = new CaptureHttpMessageHandler(_ => JsonResponse(OpenRouterResponse("openai/gpt-4o-mini", ValidAnalysisJson())));
        var provider = Provider(handler, new CreativeAnalysisOptions { Provider = "OpenRouter", Model = "", CostTier = "low" }, logger, "sk-secret-nao-logar");

        await provider.AnalyzeAsync(Request(), CancellationToken.None);

        Assert.DoesNotContain("sk-secret-nao-logar", string.Join("\n", logger.Messages));
        Assert.Contains(logger.Messages, x => x.Contains("RequestedModel=openrouter/auto", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(logger.Messages, x => x.Contains("RoutedModel=openai/gpt-4o-mini", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(logger.Messages, x => x.Contains("CostTier=low", StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        foreach (var item in environment)
        {
            Environment.SetEnvironmentVariable(item.Key, item.Value);
        }
    }

    private static OpenRouterCreativeAssetAnalysisProvider Provider(
        CaptureHttpMessageHandler handler,
        CreativeAnalysisOptions? creativeOptions = null,
        ILogger<OpenRouterCreativeAssetAnalysisProvider>? logger = null,
        string apiKey = "sk-test")
    {
        return new OpenRouterCreativeAssetAnalysisProvider(
            new StubHttpClientFactory(new HttpClient(handler)),
            Options.Create(new OpenRouterOptions { ApiKey = apiKey, BaseUrl = "https://openrouter.test", Model = "" }),
            Options.Create(creativeOptions ?? new CreativeAnalysisOptions { Provider = "OpenRouter", Model = "openrouter/auto", CostTier = "low" }),
            new Resolver(apiKey),
            logger ?? new CaptureLogger<OpenRouterCreativeAssetAnalysisProvider>());
    }

    private static CreativeAssetAnalysisProviderRequest Request()
    {
        return new CreativeAssetAnalysisProviderRequest(
            "Estetica",
            "Clinica estetica",
            "Harmonizacao facial",
            "Adultos interessados em estetica",
            "Agendar avaliacao",
            "Avaliacao personalizada",
            "Centro / Rio de Janeiro / RJ",
            "Profissional",
            ["nao prometer resultado"],
            "criativo.png",
            "image/png",
            1200,
            628,
            [137, 80, 78, 71]);
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static string OpenRouterResponse(string routedModel, string content)
    {
        return JsonSerializer.Serialize(new
        {
            id = "chatcmpl-test",
            model = routedModel,
            choices = new[]
            {
                new { message = new { content } }
            }
        });
    }

    private static string ValidAnalysisJson()
    {
        return """
        {
          "summary": "Imagem coerente com a campanha.",
          "detectedText": "Harmonizacao facial",
          "visualQualityScore": 88,
          "campaignFitScore": 90,
          "brandFitScore": 85,
          "textDensityScore": 30,
          "messageConsistencyScore": 91,
          "semanticMismatch": false,
          "placementRecommendations": {
            "facebookFeed": "recommended"
          },
          "risks": [],
          "suggestedCopy": {
            "headline": "Harmonizacao facial",
            "primaryText": "Agende sua avaliacao.",
            "description": "Atendimento personalizado.",
            "cta": "LEARN_MORE"
          }
        }
        """;
    }

    private sealed class Resolver(string apiKey) : IConfigurationResolver
    {
        public Task<ResolvedConfigurationValue> ResolveAsync(CategoriaConfiguracao categoria, string chave, CancellationToken cancellationToken)
        {
            var value = chave switch
            {
                "ApiKey" => apiKey,
                "BaseUrl" => "https://openrouter.test",
                _ => null
            };
            return Task.FromResult(new ResolvedConfigurationValue(value, value is not null, OrigemConfiguracao.AppSettings, chave == "ApiKey"));
        }

        public Task InvalidateAsync(CategoriaConfiguracao categoria, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class CaptureHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public string LastBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            return responder(request);
        }
    }

    private sealed class CaptureLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
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

[CollectionDefinition("CreativeAnalysisEnvironment", DisableParallelization = true)]
public sealed class CreativeAnalysisEnvironmentCollection;
