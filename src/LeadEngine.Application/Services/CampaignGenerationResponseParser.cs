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
            throw new CampaignGenerationException("Resposta da IA nao e um JSON valido.", ex);
        }

        var nome = Required(parsed.Nome, "nome", 180);
        var slugBase = Required(parsed.Slug, "slug", 180);
        var slug = CampanhaText.Slugify(slugBase);
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new CampaignGenerationException("Slug retornado pela IA e invalido.");
        }

        try
        {
            var conteudo = CampanhaValidator.NormalizarEValidarConteudo(
                parsed.TituloLandingPage ?? string.Empty,
                parsed.SubtituloLandingPage ?? string.Empty,
                parsed.TextoBotao ?? string.Empty,
                parsed.MensagemWhatsApp ?? string.Empty,
                parsed.Beneficios,
                parsed.PerguntasFrequentes?.Select(x => new FaqItemValidation(x.Pergunta ?? string.Empty, x.Resposta ?? string.Empty)),
                parsed.PalavrasChave,
                parsed.PalavrasChaveNegativas,
                parsed.TitulosAnuncios,
                parsed.DescricoesAnuncios);

            return new CampaignGenerationResult(
                nome,
                conteudo.TituloLandingPage,
                conteudo.SubtituloLandingPage,
                conteudo.TextoBotao,
                conteudo.MensagemWhatsApp,
                slug,
                conteudo.Beneficios,
                conteudo.PerguntasFrequentes.Select(x => new FaqItem(x.Pergunta, x.Resposta)).ToArray(),
                conteudo.PalavrasChave,
                conteudo.PalavrasChaveNegativas,
                conteudo.TitulosAnuncios,
                conteudo.DescricoesAnuncios,
                provider,
                model,
                durationMs);
        }
        catch (ArgumentException ex)
        {
            throw new CampaignGenerationException(ex.Message, ex);
        }
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
            throw new CampaignGenerationException($"Campo obrigatorio ausente na resposta da IA: {field}.");
        }

        return text;
    }

    private static JsonSerializerOptions JsonOptions() => new() { PropertyNameCaseInsensitive = true };
}
