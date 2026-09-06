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
            campanha.UrlPublica,
            campanha.SegmentId,
            campanha.Segment?.Slug,
            campanha.CampaignConfigJson,
            ToSegmentSummary(campanha),
            ToBriefing(campanha),
            SegmentMapping.UsesLegacyBriefing(campanha.Segment),
            ToLeadForm(campanha));
    }

    public static CampaignSegmentSummary? ToSegmentSummary(Campanha campanha)
    {
        return campanha.Segment is null
            ? null
            : new CampaignSegmentSummary(campanha.Segment.Id, campanha.Segment.Name, campanha.Segment.Slug, campanha.Segment.TemplateKey);
    }

    public static CampaignBriefingResponse ToBriefing(Campanha campanha)
    {
        var config = DeserializeCampaignConfig(campanha.CampaignConfigJson);
        var location = config?.Location ?? new CampaignLocationDto(campanha.Cidade, campanha.Estado, campanha.Regiao);
        return new CampaignBriefingResponse(
            config?.BusinessDescription,
            config?.ProductOrService,
            config?.TargetAudience,
            config?.CampaignGoal ?? campanha.Objetivo,
            config?.Offer,
            location,
            config?.BrandTone,
            config?.Restrictions ?? []);
    }

    public static LeadFormResponse ToLeadForm(Campanha campanha)
    {
        var form = LeadFormSchema.GetEffectiveForm(campanha);
        return new LeadFormResponse(
            form.SubmitButtonText,
            form.Fields
                .OrderBy(x => x.Order)
                .Select(x => new LeadFormFieldResponse(
                    x.Key,
                    x.Label,
                    x.Type,
                    x.Required,
                    x.Placeholder,
                    Deserialize<string>(x.OptionsJson),
                    x.DefaultValue))
                .ToArray());
    }

    private static IReadOnlyList<T> Deserialize<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<IReadOnlyList<T>>(json, JsonOptions()) ?? [];
    }

    private static CampaignContextConfig? DeserializeCampaignConfig(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<CampaignContextConfig>(json, JsonOptions());
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static JsonSerializerOptions JsonOptions() => new() { PropertyNameCaseInsensitive = true };

    private sealed record CampaignContextConfig(
        string? BusinessDescription,
        string? ProductOrService,
        string? TargetAudience,
        string? CampaignGoal,
        string? Offer,
        CampaignLocationDto? Location,
        string? BrandTone,
        IReadOnlyCollection<string>? Restrictions);
}
