using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Enums;
using Microsoft.Extensions.Options;

namespace LeadEngine.Infrastructure.CampaignGeneration;

public sealed class OpenRouterCreativeAssetAnalysisProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<OpenRouterOptions> openRouterOptions,
    IOptions<CreativeAnalysisOptions> creativeOptions,
    IConfigurationResolver resolver) : ICreativeAssetAnalysisProvider
{
    private const string ClientName = "openrouter";

    public async Task<CreativeAssetAnalysisProviderResult> AnalyzeAsync(CreativeAssetAnalysisProviderRequest request, CancellationToken cancellationToken)
    {
        var config = await EffectiveOptionsAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            throw new InvalidOperationException("OpenRouter ApiKey nao configurada para CreativeAnalysis.");
        }
        if (string.IsNullOrWhiteSpace(config.Model))
        {
            throw new InvalidOperationException("CreativeAnalysis Model nao configurado.");
        }

        var body = new
        {
            model = config.Model,
            temperature = 0.1,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = "Voce e um avaliador multimodal de criativos de campanhas. Responda somente JSON valido, sem markdown." },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = Prompt(request) },
                        new { type = "image_url", image_url = new { url = $"data:{request.MimeType};base64,{Convert.ToBase64String(request.Content)}" } }
                    }
                }
            }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{config.BaseUrl.TrimEnd('/')}/chat/completions");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
        httpRequest.Headers.TryAddWithoutValidation("HTTP-Referer", "https://leadengine.local");
        httpRequest.Headers.TryAddWithoutValidation("X-Title", "LeadEngine");
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var client = httpClientFactory.CreateClient(ClientName);
        using var response = await client.SendAsync(httpRequest, cancellationToken);
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"OpenRouter retornou HTTP {(int)response.StatusCode} na analise criativa.");
        }

        return new CreativeAssetAnalysisProviderResult("OpenRouter", config.Model, ExtractContent(text));
    }

    private async Task<OpenRouterOptions> EffectiveOptionsAsync(CancellationToken cancellationToken)
    {
        var current = openRouterOptions.Value;
        var creative = creativeOptions.Value;
        return new OpenRouterOptions
        {
            ApiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY")
                ?? (await resolver.ResolveAsync(CategoriaConfiguracao.OpenRouter, "ApiKey", cancellationToken)).Value
                ?? current.ApiKey,
            Model = Environment.GetEnvironmentVariable("CREATIVE_ANALYSIS_MODEL")
                ?? (!string.IsNullOrWhiteSpace(creative.Model) ? creative.Model : null)
                ?? (await resolver.ResolveAsync(CategoriaConfiguracao.OpenRouter, "Model", cancellationToken)).Value
                ?? current.Model,
            BaseUrl = Environment.GetEnvironmentVariable("OPENROUTER_BASE_URL")
                ?? (await resolver.ResolveAsync(CategoriaConfiguracao.OpenRouter, "BaseUrl", cancellationToken)).Value
                ?? current.BaseUrl
                ?? "https://openrouter.ai/api/v1"
        };
    }

    private static string Prompt(CreativeAssetAnalysisProviderRequest request)
    {
        return $$"""
        Analise a imagem junto com o briefing desta campanha.

        Briefing:
        - Segment: {{request.Segment ?? "-"}}
        - BusinessDescription: {{request.BusinessDescription ?? "-"}}
        - ProductOrService: {{request.ProductOrService ?? "-"}}
        - TargetAudience: {{request.TargetAudience ?? "-"}}
        - CampaignGoal: {{request.CampaignGoal ?? "-"}}
        - Offer: {{request.Offer ?? "-"}}
        - Location: {{request.Location ?? "-"}}
        - BrandTone: {{request.BrandTone ?? "-"}}
        - Restrictions: {{string.Join("; ", request.Restrictions)}}

        Avalie sem usar regras fixas por segmento.
        campaignFitScore mede: esta imagem representa corretamente o produto/servico, publico e objetivo desta campanha?
        messageConsistencyScore mede: o texto e conteudo visual detectados combinam com o briefing desta campanha?
        Se a imagem ou texto promoverem outro produto/servico, semanticMismatch deve ser true e os scores de aderencia devem ser baixos.

        Responda exatamente neste formato JSON:
        {
          "summary": "string curta",
          "detectedText": "texto visivel detectado ou string vazia",
          "visualQualityScore": 0,
          "campaignFitScore": 0,
          "brandFitScore": 0,
          "textDensityScore": 0,
          "messageConsistencyScore": 0,
          "semanticMismatch": false,
          "placementRecommendations": {
            "facebookFeed": "recommended|needs_crop|not_recommended",
            "instagramFeed": "recommended|needs_crop|not_recommended",
            "stories": "recommended|needs_crop|not_recommended",
            "reels": "recommended|needs_crop|not_recommended"
          },
          "risks": ["string"],
          "suggestedCopy": {
            "headline": "string",
            "primaryText": "string",
            "description": "string",
            "cta": "LEARN_MORE"
          }
        }
        """;
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
                ?? throw new InvalidOperationException("OpenRouter retornou conteudo vazio na analise criativa.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Resposta HTTP do OpenRouter nao e JSON valido.", ex);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Resposta do OpenRouter nao possui conteudo esperado.", ex);
        }
    }
}
