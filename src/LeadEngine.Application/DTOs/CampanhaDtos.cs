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
    string? Objetivo,
    string? SegmentSlug = null,
    string? BusinessDescription = null,
    string? ProductOrService = null,
    string? TargetAudience = null,
    string? CampaignGoal = null,
    string? Offer = null,
    CampaignLocationDto? Location = null,
    string? BrandTone = null,
    IReadOnlyCollection<string>? Restrictions = null);

public sealed record CampaignLocationDto(
    string? City,
    string? State,
    string? Region);

public sealed record CampaignSegmentSummary(
    Guid? Id,
    string? Name,
    string? Slug,
    string? TemplateKey);

public sealed record CampaignBriefingResponse(
    string? BusinessDescription,
    string? ProductOrService,
    string? TargetAudience,
    string? CampaignGoal,
    string? Offer,
    CampaignLocationDto? Location,
    string? BrandTone,
    IReadOnlyCollection<string> Restrictions);

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
    IReadOnlyList<string> DescricoesAnuncios,
    LeadFormResponse? Form = null);

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
    string? UrlPublica,
    Guid? SegmentId,
    string? SegmentSlug,
    string? CampaignConfigJson,
    CampaignSegmentSummary? Segment,
    CampaignBriefingResponse Briefing,
    bool UsesLegacyBriefing,
    LeadFormResponse Form);

public sealed record FaqResponse(string Pergunta, string Resposta);

public sealed record LeadFormResponse(
    string SubmitButtonText,
    IReadOnlyList<LeadFormFieldResponse> Fields);

public sealed record LeadFormFieldResponse(
    string Key,
    string Label,
    string Type,
    bool Required,
    string? Placeholder,
    IReadOnlyList<string> Options,
    string? DefaultValue);

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
    string MensagemBaseWhatsApp,
    CampaignSegmentSummary? Segment,
    CampaignBriefingResponse Briefing,
    bool UsesLegacyBriefing,
    LeadFormResponse Form);
