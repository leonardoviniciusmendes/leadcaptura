using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Infrastructure.GoogleAds;

public sealed class OpenRouterGoogleAdsCopyAdjustmentService(
    IHttpClientFactory httpClientFactory,
    IConfigurationResolver resolver) : IGoogleAdsCopyAdjustmentService
{
    public async Task<IReadOnlyList<GoogleAdsCopySuggestionItem>> SugerirAsync(GoogleAdsPreviewPayload payload, IReadOnlyList<string> campos, CancellationToken cancellationToken)
    {
        var apiKey = (await resolver.ResolveAsync(CategoriaConfiguracao.OpenRouter, "ApiKey", cancellationToken)).Value;
        var model = (await resolver.ResolveAsync(CategoriaConfiguracao.OpenRouter, "Model", cancellationToken)).Value;
        var baseUrl = (await resolver.ResolveAsync(CategoriaConfiguracao.OpenRouter, "BaseUrl", cancellationToken)).Value ?? "https://openrouter.ai/api/v1";
        var temperature = double.TryParse((await resolver.ResolveAsync(CategoriaConfiguracao.OpenRouter, "Temperature", cancellationToken)).Value, out var t) ? t : 0.2;
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(model))
        {
            throw new InvalidOperationException("OpenRouter precisa estar configurado para sugerir ajustes.");
        }

        var rsa = payload.AdGroups.First().ResponsiveSearchAd;
        var oversized = new List<object>();
        if (campos.Any(x => x.Equals("headlines", StringComparison.OrdinalIgnoreCase)))
        {
            oversized.AddRange(rsa.Headlines.Select((text, index) => new { campo = "headlines", indice = index, original = text, limite = 30 }).Where(x => x.original.Length > x.limite));
        }
        if (campos.Any(x => x.Equals("descriptions", StringComparison.OrdinalIgnoreCase)))
        {
            oversized.AddRange(rsa.Descriptions.Select((text, index) => new { campo = "descriptions", indice = index, original = text, limite = 90 }).Where(x => x.original.Length > x.limite));
        }
        if (oversized.Count == 0)
        {
            return [];
        }

        var body = new
        {
            model,
            temperature,
            response_format = new { type = "json_object" },
            messages = new[]
            {
                new { role = "system", content = "Voce ajusta copies de Google Ads e responde somente JSON valido." },
                new { role = "user", content = Prompt(oversized) }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Headers.TryAddWithoutValidation("HTTP-Referer", "https://leadengine.local");
        request.Headers.TryAddWithoutValidation("X-Title", "LeadEngine");
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        using var response = await httpClientFactory.CreateClient("openrouter").SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new CampaignGenerationException("Nao foi possivel gerar sugestoes de ajuste.");
        }

        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        return Parse(text);
    }

    private static string Prompt(IReadOnlyList<object> oversized)
    {
        return "Sugira versoes menores para os campos abaixo.\n"
            + "Preserve intencao comercial, sem promessas de preco, aprovacao, cobertura ou carencia.\n"
            + "Respeite exatamente o limite informado.\n"
            + "Nao substitua automaticamente.\n"
            + "Saida exclusivamente em JSON neste formato: "
            + "{ \"sugestoes\": [{ \"campo\": \"headlines\", \"indice\": 0, \"original\": \"texto\", \"sugestao\": \"texto menor\", \"limite\": 30 }] }\n\n"
            + "Campos:\n"
            + JsonSerializer.Serialize(oversized);
    }

    private static IReadOnlyList<GoogleAdsCopySuggestionItem> Parse(string responseText)
    {
        using var doc = JsonDocument.Parse(responseText);
        var content = doc.RootElement.TryGetProperty("choices", out var choices)
            ? choices[0].GetProperty("message").GetProperty("content").GetString() ?? "{}"
            : responseText;
        using var inner = JsonDocument.Parse(StripCodeFence(content));
        if (!inner.RootElement.TryGetProperty("sugestoes", out var sugestoes) || sugestoes.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return sugestoes.EnumerateArray().Select(x => new GoogleAdsCopySuggestionItem(
            ReadString(x, "campo"),
            x.TryGetProperty("indice", out var indice) && indice.TryGetInt32(out var index) ? index : 0,
            ReadString(x, "original"),
            ReadString(x, "sugestao"),
            x.TryGetProperty("limite", out var limite) && limite.TryGetInt32(out var limit) ? limit : 0)).ToArray();
    }

    private static string StripCodeFence(string content)
    {
        var trimmed = content.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal)) return trimmed;
        var lines = trimmed.Split('\n');
        var end = lines.Length > 0 && lines[^1].Trim() == "```" ? lines.Length - 1 : lines.Length;
        return string.Join('\n', lines[1..end]).Trim();
    }

    private static string ReadString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;
    }
}
