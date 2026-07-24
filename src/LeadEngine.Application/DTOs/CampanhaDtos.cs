using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.DTOs;

public sealed record GerarCampanhaRequest(
    TipoPublicoCampanha TipoPublico,
    string Cidade,
    string Estado,
    string? Regiao,
    string Operadora,
    string? OperadoraOutra,
    decimal OrcamentoDiario,
    string? Objetivo);

public sealed record RevisarCampanhaRequest(
    string Nome,
    string TituloLandingPage,
    string SubtituloLandingPage,
    string TextoBotao,
    string MensagemWhatsApp,
    string Slug,
    string? Objetivo,
    StatusCampanha Status);

public sealed record CampanhaResponse(
    Guid Id,
    string Nome,
    TipoPublicoCampanha TipoPublico,
    string Cidade,
    string Estado,
    string? Regiao,
    string Operadora,
    decimal OrcamentoDiario,
    string? Objetivo,
    StatusCampanha Status,
    string TituloLandingPage,
    string SubtituloLandingPage,
    string TextoBotao,
    string MensagemWhatsApp,
    string Slug,
    IReadOnlyList<string> Beneficios,
    IReadOnlyList<FaqResponse> PerguntasFrequentes,
    IReadOnlyList<string> PalavrasChave,
    IReadOnlyList<string> PalavrasChaveNegativas,
    IReadOnlyList<string> TitulosAnuncios,
    IReadOnlyList<string> DescricoesAnuncios,
    string? ErroGeracao,
    string? ProviderIa,
    string? ModeloIa,
    DateTime? DataGeracao,
    long? DuracaoGeracaoMs,
    DateTime DataCriacao,
    DateTime? DataAtualizacao);

public sealed record FaqResponse(string Pergunta, string Resposta);
