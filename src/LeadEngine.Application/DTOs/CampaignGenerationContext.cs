using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.DTOs;

public sealed record CampaignGenerationContext(
    string? SegmentName,
    string? SegmentSlug,
    string? TemplateKey,
    string? SegmentDefaultConfig,
    string? CampaignConfigJson,
    string? BusinessDescription,
    string? ProductOrService,
    string? TargetAudience,
    string? CampaignGoal,
    string? Offer,
    CampaignLocationDto? Location,
    string? BrandTone,
    IReadOnlyCollection<string>? Restrictions,
    CampaignGenerationLegacyContext LegacyContext);

public sealed record CampaignGenerationLegacyContext(
    TipoPublicoCampanha TipoPublico,
    string? Cidade,
    string? Estado,
    string? Regiao,
    string? Operadora,
    string? OperadoraOutra,
    decimal OrcamentoDiario,
    string? Objetivo);
