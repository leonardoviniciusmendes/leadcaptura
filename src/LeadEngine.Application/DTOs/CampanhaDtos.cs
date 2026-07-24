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
    IReadOnlyList<string> Beneficios,
    IReadOnlyList<FaqResponse> PerguntasFrequentes,
    IReadOnlyList<string> PalavrasChave,
    IReadOnlyList<string> PalavrasChaveNegativas,
    IReadOnlyList<string> TitulosAnuncios,
    IReadOnlyList<string> DescricoesAnuncios);

public sealed record RegenerarCampanhaSecaoRequest(
    CampanhaSecao Secao,
    string? InstrucaoAdicional);

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
    DateTime? DataAtualizacao,
    bool Publicada,
    bool Ativo,
    DateTime? DataPublicacao,
    DateTime? DataDespublicacao,
    string? UrlPublica);

public sealed record FaqResponse(string Pergunta, string Resposta);

public sealed record CampanhaRevisaoHistoricoResponse(
    DateTime Data,
    CampanhaSecao? Secao,
    OrigemRevisaoCampanha Origem,
    string ResumoAlteracao,
    string? Provider,
    string? Modelo);

public sealed record CampanhaPublicacaoResponse(
    Guid Id,
    StatusCampanha Status,
    bool Publicada,
    bool Ativo,
    DateTime? DataPublicacao,
    DateTime? DataDespublicacao,
    string? SlugPublico,
    string? UrlPublica);

public sealed record CampanhaPublicaResponse(
    string Nome,
    string Titulo,
    string Subtitulo,
    string TextoBotao,
    IReadOnlyList<string> Beneficios,
    IReadOnlyList<FaqResponse> PerguntasFrequentes,
    string Operadora,
    string Cidade,
    string Estado,
    TipoPublicoCampanha TipoPublico,
    string MensagemBaseWhatsApp);
