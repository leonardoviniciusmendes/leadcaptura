using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeadEngine.Infrastructure.CampaignGeneration;

public sealed class OpenRouterCampaignGenerationService(
    IHttpClientFactory httpClientFactory,
    IOptions<OpenRouterOptions> options,
    CampaignPromptBuilder promptBuilder,
    CampaignGenerationResponseParser parser,
    ILogger<OpenRouterCampaignGenerationService> logger,
    IConfigurationResolver? resolver = null) : ICampaignGenerationService
{
    private const string ClientName = "openrouter";

    public async Task<CampaignGenerationResult> GenerateAsync(GerarCampanhaRequest briefing, CancellationToken cancellationToken)
    {
        var config = await EffectiveOptionsAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            throw new CampaignGenerationException("OpenRouter ApiKey não configurada.");
        }

        if (string.IsNullOrWhiteSpace(config.Model))
        {
            throw new CampaignGenerationException("OpenRouter Model não configurado.");
        }

        var prompt = promptBuilder.Build(briefing);
        var body = new
        {
            model = config.Model,
            temperature = config.Temperature,
            response_format = new { type = "json_object" },
            messages = new[]
            {
                new { role = "system", content = "Você gera campanhas de Google Ads para planos de saúde e responde somente JSON válido." },
                new { role = "user", content = prompt }
            }
        };

        var sw = Stopwatch.StartNew();
        var responseText = await SendWithRetryAsync(config, body, cancellationToken);
        sw.Stop();

        var content = ExtractContent(responseText);
        return parser.Parse(content, "OpenRouter", config.Model, sw.ElapsedMilliseconds);
    }

    public async Task<OpenRouterTestResult> TestarConectividadeAsync(CancellationToken cancellationToken)
    {
        var config = options.Value;
        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            throw new CampaignGenerationException("OpenRouter ApiKey não configurada.");
        }

        if (string.IsNullOrWhiteSpace(config.Model))
        {
            throw new CampaignGenerationException("OpenRouter Model não configurado.");
        }

        var body = new
        {
            model = config.Model,
            temperature = 0,
            messages = new[]
            {
                new { role = "user", content = "Responda apenas: ok" }
            }
        };

        var sw = Stopwatch.StartNew();
        await SendWithRetryAsync(config, body, cancellationToken);
        sw.Stop();
        return new OpenRouterTestResult("OpenRouter", config.Model, sw.ElapsedMilliseconds);
    }

    private async Task<OpenRouterOptions> EffectiveOptionsAsync(CancellationToken cancellationToken)
    {
        var current = options.Value;
        return new OpenRouterOptions
        {
            ApiKey = await ResolveValueAsync("ApiKey", current.ApiKey, cancellationToken) ?? string.Empty,
            Model = await ResolveValueAsync("Model", current.Model, cancellationToken) ?? string.Empty,
            BaseUrl = await ResolveValueAsync("BaseUrl", current.BaseUrl, cancellationToken) ?? "https://openrouter.ai/api/v1",
            TimeoutSeconds = ParseInt(await ResolveValueAsync("TimeoutSeconds", current.TimeoutSeconds.ToString(), cancellationToken), current.TimeoutSeconds),
            MaxRetries = ParseInt(await ResolveValueAsync("MaxRetries", current.MaxRetries.ToString(), cancellationToken), current.MaxRetries),
            Temperature = ParseDouble(await ResolveValueAsync("Temperature", current.Temperature.ToString(), cancellationToken), current.Temperature)
        };
    }

    private async Task<string?> ResolveValueAsync(string key, string? fallback, CancellationToken cancellationToken)
    {
        return resolver is null ? fallback : (await resolver.ResolveAsync(CategoriaConfiguracao.OpenRouter, key, cancellationToken)).Value ?? fallback;
    }

    private static int ParseInt(string? value, int fallback) => int.TryParse(value, out var parsed) ? parsed : fallback;
    private static double ParseDouble(string? value, double fallback) => double.TryParse(value, out var parsed) ? parsed : fallback;

    private async Task<string> SendWithRetryAsync(OpenRouterOptions config, object body, CancellationToken cancellationToken)
    {
        var attempts = Math.Max(0, config.MaxRetries) + 1;
        var timeout = TimeSpan.FromSeconds(Math.Clamp(config.TimeoutSeconds, 5, 300));
        Exception? lastException = null;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);
        var operationToken = timeoutCts.Token;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                using var request = CreateRequest(config, body);
                var client = httpClientFactory.CreateClient(ClientName);
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, operationToken);
                var text = await response.Content.ReadAsStringAsync(operationToken);

                if (response.IsSuccessStatusCode)
                {
                    return text;
                }

                if (!ShouldRetry(response.StatusCode) || attempt == attempts)
                {
                    logger.LogWarning("OpenRouter retornou HTTP {StatusCode}.", (int)response.StatusCode);
                    throw new CampaignGenerationException($"OpenRouter retornou HTTP {(int)response.StatusCode}.");
                }

                await DelayAsync(attempt, operationToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                lastException = new TimeoutException("Tempo limite excedido ao chamar OpenRouter.");
                if (attempt == attempts)
                {
                    throw new CampaignGenerationException("Tempo limite excedido ao chamar OpenRouter.", lastException);
                }
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                if (attempt == attempts)
                {
                    throw new CampaignGenerationException("Falha transitória ao chamar OpenRouter.", ex);
                }
            }
        }

        throw new CampaignGenerationException("Não foi possível chamar OpenRouter.", lastException);
    }

    private static HttpRequestMessage CreateRequest(OpenRouterOptions config, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{config.BaseUrl.TrimEnd('/')}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
        request.Headers.TryAddWithoutValidation("HTTP-Referer", "https://leadengine.local");
        request.Headers.TryAddWithoutValidation("X-Title", "LeadEngine");
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        return request;
    }

    private static bool ShouldRetry(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests
            || (int)statusCode >= 500;
    }

    private static Task DelayAsync(int attempt, CancellationToken cancellationToken)
    {
        return Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), cancellationToken);
    }

    private static string ExtractContent(string responseText)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseText);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()
                ?? throw new CampaignGenerationException("OpenRouter retornou conteúdo vazio.");
        }
        catch (KeyNotFoundException ex)
        {
            throw new CampaignGenerationException("Resposta do OpenRouter não possui conteúdo esperado.", ex);
        }
        catch (InvalidOperationException ex)
        {
            throw new CampaignGenerationException("Resposta do OpenRouter não possui conteúdo esperado.", ex);
        }
        catch (JsonException ex)
        {
            throw new CampaignGenerationException("Resposta HTTP do OpenRouter não é JSON válido.", ex);
        }
    }
}

public sealed record OpenRouterTestResult(string Provider, string Modelo, long DuracaoMs);
