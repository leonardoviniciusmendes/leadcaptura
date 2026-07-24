using System.Text.Json;
using LeadEngine.Application.DTOs;
using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Services;

public static class CampanhaMapping
{
    public static CampanhaResponse ToResponse(Campanha campanha)
    {
        return new CampanhaResponse(
            campanha.Id,
            campanha.Nome,
            campanha.TipoPublico,
            campanha.Cidade,
            campanha.Estado,
            campanha.Regiao,
            campanha.Operadora,
            campanha.OrcamentoDiario,
            campanha.Objetivo,
            campanha.Status,
            campanha.TituloLandingPage,
            campanha.SubtituloLandingPage,
            campanha.TextoBotao,
            campanha.MensagemWhatsApp,
            campanha.Slug,
            Deserialize<string>(campanha.BeneficiosJson),
            Deserialize<FaqResponse>(campanha.PerguntasFrequentesJson),
            Deserialize<string>(campanha.PalavrasChaveJson),
            Deserialize<string>(campanha.PalavrasChaveNegativasJson),
            Deserialize<string>(campanha.TitulosAnunciosJson),
            Deserialize<string>(campanha.DescricoesAnunciosJson),
            campanha.ErroGeracao,
            campanha.ProviderIa,
            campanha.ModeloIa,
            campanha.DataGeracao,
            campanha.DuracaoGeracaoMs,
            campanha.DataCriacao,
            campanha.DataAtualizacao,
            campanha.Publicada,
            campanha.Ativo,
            campanha.DataPublicacao,
            campanha.DataDespublicacao,
            campanha.UrlPublica);
    }

    private static IReadOnlyList<T> Deserialize<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<IReadOnlyList<T>>(json, JsonOptions()) ?? [];
    }

    private static JsonSerializerOptions JsonOptions() => new() { PropertyNameCaseInsensitive = true };
}
