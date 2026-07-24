using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LeadEngine.Application.Common;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeadEngine.Infrastructure.CampaignGeneration;

public sealed class OpenRouterCampaignSectionGenerationService(
    IHttpClientFactory httpClientFactory,
    IOptions<OpenRouterOptions> options,
    CampaignSectionPromptBuilder promptBuilder,
    CampaignSectionResponseParser parser,
    ILogger<OpenRouterCampaignSectionGenerationService> logger,
    IConfigurationResolver? resolver = null) : ICampaignSectionGenerationService
{
    private const string ClientName = "openrouter";

    public async Task<CampaignSectionGenerationResult> GenerateAsync(
        Campanha campanha,
        CampanhaSecao secao,
        string? instrucaoAdicional,
        CancellationToken cancellationToken)
    {
        var config = await EffectiveOptionsAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            throw new CampaignGenerationException("OpenRouter ApiKey nao configurada.");
        }

        if (string.IsNullOrWhiteSpace(config.Model))
        {
            throw new CampaignGenerationException("OpenRouter Model nao configurado.");
        }

        var prompt = promptBuilder.Build(campanha, secao, instrucaoAdicional);
        var body = new
        {
            model = config.Model,
            temperature = config.Temperature,
            response_format = new { type = "json_object" },
            messages = new[]
            {
                new { role = "system", content = "Voce revisa secoes de campanhas de Google Ads para planos de saude e responde somente JSON valido." },
                new { role = "user", content = prompt }
            }
        };

        var sw = Stopwatch.StartNew();
        var responseText = await SendWithRetryAsync(config, body, cancellationToken);
        sw.Stop();

        var content = ExtractContent(responseText);
        var parsed = parser.Parse(content, secao, CampanhaContentSnapshot.From(campanha));
        return new CampaignSectionGenerationResult(secao, parsed, "OpenRouter", config.Model);
    }

    private async Task<OpenRouterOptions> EffectiveOptionsAsync(CancellationToken cancellationToken)
    {
        var current = options.Value;
        return new OpenRouterOptions
        {
            ApiKey = await ResolveValueAsync("ApiKey", current.ApiKey, cancellationToken) ?? string.Empty,
            Model = await ResolveValueAsync("Model", current.Model, cancellationToken) ?? string.Empty,
            BaseUrl = await ResolveValueAsync("BaseUrl", current.BaseUrl, cancellationToken) ?? "https://openrouter.ai/api/v1",
            TimeoutSeconds = int.TryParse(await ResolveValueAsync("TimeoutSeconds", current.TimeoutSeconds.ToString(), cancellationToken), out var timeout) ? timeout : current.TimeoutSeconds,
            MaxRetries = int.TryParse(await ResolveValueAsync("MaxRetries", current.MaxRetries.ToString(), cancellationToken), out var retries) ? retries : current.MaxRetries,
            Temperature = double.TryParse(await ResolveValueAsync("Temperature", current.Temperature.ToString(), cancellationToken), out var temp) ? temp : current.Temperature
        };
    }

    private async Task<string?> ResolveValueAsync(string key, string? fallback, CancellationToken cancellationToken)
    {
        return resolver is null ? fallback : (await resolver.ResolveAsync(CategoriaConfiguracao.OpenRouter, key, cancellationToken)).Value ?? fallback;
    }

    private async Task<string> SendWithRetryAsync(OpenRouterOptions config, object body, CancellationToken cancellationToken)
    {
        var attempts = Math.Max(0, config.MaxRetries) + 1;
        Exception? lastException = null;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(config.TimeoutSeconds, 1, 300)));

            try
            {
                using var request = CreateRequest(config, body);
                var client = httpClientFactory.CreateClient(ClientName);
                var response = await client.SendAsync(request, timeoutCts.Token);
                var text = await response.Content.ReadAsStringAsync(timeoutCts.Token);

                if (response.IsSuccessStatusCode)
                {
                    return text;
                }

                if (!ShouldRetry(response.StatusCode) || attempt == attempts)
                {
                    logger.LogWarning("OpenRouter retornou HTTP {StatusCode} na regeneracao parcial.", (int)response.StatusCode);
                    throw new CampaignGenerationException($"OpenRouter retornou HTTP {(int)response.StatusCode}.");
                }

                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), timeoutCts.Token);
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
                    throw new CampaignGenerationException("Falha transitoria ao chamar OpenRouter.", ex);
                }
            }
        }

        throw new CampaignGenerationException("Nao foi possivel chamar OpenRouter.", lastException);
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
                ?? throw new CampaignGenerationException("OpenRouter retornou conteudo vazio.");
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException or JsonException)
        {
            throw new CampaignGenerationException("Resposta do OpenRouter nao possui conteudo esperado.", ex);
        }
    }
}
