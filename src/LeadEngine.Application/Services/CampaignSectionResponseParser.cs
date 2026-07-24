using System.Text.Json;
using LeadEngine.Application.Common;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Services;

public sealed class CampaignSectionResponseParser
{
    public object Parse(string content, CampanhaSecao secao, CampanhaContentSnapshot atual)
    {
        try
        {
            using var doc = JsonDocument.Parse(StripCodeFence(content));
            var root = doc.RootElement;
            return secao switch
            {
                CampanhaSecao.Nome => RequiredString(root, "nome", 180),
                CampanhaSecao.LandingPage => new LandingPageSection(
                    RequiredString(root, "tituloLandingPage", 180),
                    RequiredString(root, "subtituloLandingPage", 300),
                    RequiredString(root, "textoBotao", 80)),
                CampanhaSecao.MensagemWhatsApp => RequiredString(root, "mensagemWhatsApp", 500),
                CampanhaSecao.Beneficios => ReadStringArray(root, "beneficios", 120),
                CampanhaSecao.PerguntasFrequentes => ReadFaq(root, "perguntasFrequentes"),
                CampanhaSecao.PalavrasChave => ReadStringArray(root, "palavrasChave", 120),
                CampanhaSecao.PalavrasChaveNegativas => ReadStringArray(root, "palavrasChaveNegativas", 120),
                CampanhaSecao.TitulosAnuncios => ReadStringArray(root, "titulosAnuncios", 30),
                CampanhaSecao.DescricoesAnuncios => ReadStringArray(root, "descricoesAnuncios", 90),
                _ => throw new ArgumentException("Secao invalida.")
            };
        }
        catch (JsonException ex)
        {
            throw new CampaignGenerationException("Resposta da IA nao e um JSON valido.", ex);
        }
        catch (KeyNotFoundException ex)
        {
            throw new CampaignGenerationException("Resposta da IA nao possui o campo esperado.", ex);
        }
        catch (InvalidOperationException ex)
        {
            throw new CampaignGenerationException("Resposta da IA nao possui o formato esperado.", ex);
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
        var end = lines.Length;
        if (lines[^1].Trim().Equals("```", StringComparison.Ordinal))
        {
            end--;
        }

        return string.Join('\n', lines[1..end]).Trim();
    }

    private static string RequiredString(JsonElement root, string property, int max)
    {
        var text = CampanhaText.Limitar(root.GetProperty(property).GetString(), max);
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new CampaignGenerationException($"Campo obrigatorio ausente na resposta da IA: {property}.");
        }

        return text;
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement root, string property, int max)
    {
        return root.GetProperty(property)
            .EnumerateArray()
            .Select(x => CampanhaText.Limitar(x.GetString(), max))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToArray();
    }

    private static IReadOnlyList<FaqItemValidation> ReadFaq(JsonElement root, string property)
    {
        return root.GetProperty(property)
            .EnumerateArray()
            .Select(x => new FaqItemValidation(
                CampanhaText.Limitar(x.GetProperty("pergunta").GetString(), 180) ?? string.Empty,
                CampanhaText.Limitar(x.GetProperty("resposta").GetString(), 500) ?? string.Empty))
            .ToArray();
    }
}

public sealed record LandingPageSection(string TituloLandingPage, string SubtituloLandingPage, string TextoBotao);
