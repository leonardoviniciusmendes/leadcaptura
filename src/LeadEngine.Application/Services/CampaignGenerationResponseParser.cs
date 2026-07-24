using System.Text.Json;
using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;

namespace LeadEngine.Application.Services;

public sealed class CampaignGenerationResponseParser
{
    public CampaignGenerationResult Parse(string content, string provider, string model, long durationMs)
    {
        var json = StripCodeFence(content);
        CampaignGenerationAiResponse parsed;

        try
        {
            parsed = JsonSerializer.Deserialize<CampaignGenerationAiResponse>(json, JsonOptions())
                ?? throw new CampaignGenerationException("Resposta vazia do provedor de IA.");
        }
        catch (JsonException ex)
        {
            throw new CampaignGenerationException("Resposta da IA não é um JSON válido.", ex);
        }

        var nome = Required(parsed.Nome, "nome", 180);
        var titulo = Required(parsed.TituloLandingPage, "tituloLandingPage", 180);
        var subtitulo = Required(parsed.SubtituloLandingPage, "subtituloLandingPage", 300);
        var textoBotao = Required(parsed.TextoBotao, "textoBotao", 80);
        var mensagem = Required(parsed.MensagemWhatsApp, "mensagemWhatsApp", 500);
        var slugBase = Required(parsed.Slug, "slug", 180);
        var slug = CampanhaText.Slugify(slugBase);
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new CampaignGenerationException("Slug retornado pela IA é inválido.");
        }

        var beneficios = NormalizeList(parsed.Beneficios, "beneficios", 3, 6, 120);
        var faq = NormalizeFaq(parsed.PerguntasFrequentes, 3, 6);
        var palavrasChave = NormalizeList(parsed.PalavrasChave, "palavrasChave", 1, 30, 120);
        var negativas = NormalizeList(parsed.PalavrasChaveNegativas, "palavrasChaveNegativas", 1, 40, 120);
        var titulos = NormalizeList(parsed.TitulosAnuncios, "titulosAnuncios", 8, 12, 30);
        var descricoes = NormalizeList(parsed.DescricoesAnuncios, "descricoesAnuncios", 3, 4, 90);

        return new CampaignGenerationResult(
            nome,
            titulo,
            subtitulo,
            textoBotao,
            mensagem,
            slug,
            beneficios,
            faq,
            palavrasChave,
            negativas,
            titulos,
            descricoes,
            provider,
            model,
            durationMs);
    }

    private static string StripCodeFence(string content)
    {
        var trimmed = content.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var lines = trimmed.Split('\n');
        if (lines.Length < 2)
        {
            return trimmed;
        }

        var start = 1;
        var end = lines.Length;
        if (lines[^1].Trim().Equals("```", StringComparison.Ordinal))
        {
            end--;
        }

        return string.Join('\n', lines[start..end]).Trim();
    }

    private static string Required(string? value, string field, int maxLength)
    {
        var text = CampanhaText.Limitar(value, maxLength);
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new CampaignGenerationException($"Campo obrigatório ausente na resposta da IA: {field}.");
        }

        return text;
    }

    private static IReadOnlyList<string> NormalizeList(List<string>? values, string field, int min, int max, int itemMaxLength)
    {
        var items = (values ?? [])
            .Select(x => CampanhaText.Limitar(x, itemMaxLength))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .Take(max)
            .ToArray();

        if (items.Length < min)
        {
            throw new CampaignGenerationException($"Campo {field} deve conter entre {min} e {max} itens válidos.");
        }

        return items;
    }

    private static IReadOnlyList<FaqItem> NormalizeFaq(List<CampaignGenerationAiFaq>? values, int min, int max)
    {
        var items = (values ?? [])
            .Select(x => new FaqItem(
                Required(x.Pergunta, "perguntasFrequentes.pergunta", 180),
                Required(x.Resposta, "perguntasFrequentes.resposta", 500)))
            .Take(max)
            .ToArray();

        if (items.Length < min)
        {
            throw new CampaignGenerationException($"FAQ deve conter entre {min} e {max} itens válidos.");
        }

        return items;
    }

    private static JsonSerializerOptions JsonOptions() => new() { PropertyNameCaseInsensitive = true };
}
