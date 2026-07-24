using System.Text.Json;
using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Services;

public sealed record CampanhaContentSnapshot(
    string Nome,
    string TituloLandingPage,
    string SubtituloLandingPage,
    string TextoBotao,
    string MensagemWhatsApp,
    IReadOnlyList<string> Beneficios,
    IReadOnlyList<FaqSnapshot> PerguntasFrequentes,
    IReadOnlyList<string> PalavrasChave,
    IReadOnlyList<string> PalavrasChaveNegativas,
    IReadOnlyList<string> TitulosAnuncios,
    IReadOnlyList<string> DescricoesAnuncios)
{
    public static CampanhaContentSnapshot From(Campanha campanha)
    {
        return new CampanhaContentSnapshot(
            campanha.Nome,
            campanha.TituloLandingPage,
            campanha.SubtituloLandingPage,
            campanha.TextoBotao,
            campanha.MensagemWhatsApp,
            Deserialize<string>(campanha.BeneficiosJson),
            Deserialize<FaqSnapshot>(campanha.PerguntasFrequentesJson),
            Deserialize<string>(campanha.PalavrasChaveJson),
            Deserialize<string>(campanha.PalavrasChaveNegativasJson),
            Deserialize<string>(campanha.TitulosAnunciosJson),
            Deserialize<string>(campanha.DescricoesAnunciosJson));
    }

    private static IReadOnlyList<T> Deserialize<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<IReadOnlyList<T>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }
}

public sealed record FaqSnapshot(string Pergunta, string Resposta);
