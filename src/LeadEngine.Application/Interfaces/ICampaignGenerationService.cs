using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Interfaces;

public interface ICampaignGenerationService
{
    Task<CampaignGenerationResult> GenerateAsync(GerarCampanhaRequest briefing, CancellationToken cancellationToken);
}

public sealed record CampaignGenerationResult(
    string Nome,
    string TituloLandingPage,
    string SubtituloLandingPage,
    string TextoBotao,
    string MensagemWhatsApp,
    string Slug,
    IReadOnlyList<string> Beneficios,
    IReadOnlyList<FaqItem> PerguntasFrequentes,
    IReadOnlyList<string> PalavrasChave,
    IReadOnlyList<string> PalavrasChaveNegativas,
    IReadOnlyList<string> TitulosAnuncios,
    IReadOnlyList<string> DescricoesAnuncios,
    string Provider,
    string Modelo,
    long DuracaoMs);

public sealed record FaqItem(string Pergunta, string Resposta);
