using System.Text.Json;
using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Services;

public sealed class CampanhaService(
    ICampanhaRepository repository,
    ICampaignGenerationService generationService,
    ISegmentRepository? segmentRepository = null)
{
    private const string DefaultSegmentSlug = "planos-saude";

    public async Task<CampanhaResponse> GerarCampanhaAsync(GerarCampanhaRequest request, CancellationToken cancellationToken)
    {
        CampanhaValidator.ValidarBriefing(request);
        var segment = await ResolveSegmentAsync(request.SegmentSlug, cancellationToken);
        var campaignConfigJson = BuildCampaignConfigJson(request);
        var generationContext = CampaignGenerationContextFactory.FromRequest(request, segment, campaignConfigJson);

        var now = DateTime.UtcNow;
        var campanha = new Campanha
        {
            Id = Guid.NewGuid(),
            Nome = "Campanha em geracao",
            SegmentId = segment?.Id,
            Segment = segment,
            TipoPublico = request.TipoPublico,
            Cidade = SafeText(generationContext.Location?.City ?? request.Cidade, "Nao informado"),
            Estado = SafeText(generationContext.Location?.State ?? request.Estado, "NA").ToUpperInvariant(),
            Regiao = CampanhaText.Limitar(generationContext.Location?.Region ?? request.Regiao, 120),
            Operadora = CampanhaValidator.OperadoraEfetiva(request),
            OrcamentoDiario = request.OrcamentoDiario,
            Objetivo = CampanhaText.Limitar(request.Objetivo, 500),
            CampaignConfigJson = campaignConfigJson,
            Status = StatusCampanha.Gerando,
            Slug = $"campanha-{Guid.NewGuid():N}"[..17],
            DataCriacao = now
        };

        campanha.LeadForms.Add(LeadFormSchema.CreateForCampaign(campanha));

        await repository.AdicionarAsync(campanha, cancellationToken);
        await repository.SalvarAsync(cancellationToken);

        var finalizationToken = CancellationToken.None;
        try
        {
            var generated = await generationService.GenerateAsync(generationContext, finalizationToken);
            campanha.Nome = generated.Nome;
            campanha.TituloLandingPage = generated.TituloLandingPage;
            campanha.SubtituloLandingPage = generated.SubtituloLandingPage;
            campanha.TextoBotao = generated.TextoBotao;
            campanha.MensagemWhatsApp = generated.MensagemWhatsApp;
            campanha.Slug = await EnsureUniqueSlugAsync(generated.Slug, campanha.Id, finalizationToken);
            campanha.BeneficiosJson = Serialize(generated.Beneficios);
            campanha.PerguntasFrequentesJson = Serialize(generated.PerguntasFrequentes.Select(x => new FaqResponse(x.Pergunta, x.Resposta)).ToArray());
            campanha.PalavrasChaveJson = Serialize(generated.PalavrasChave);
            campanha.PalavrasChaveNegativasJson = Serialize(generated.PalavrasChaveNegativas);
            campanha.TitulosAnunciosJson = Serialize(generated.TitulosAnuncios);
            campanha.DescricoesAnunciosJson = Serialize(generated.DescricoesAnuncios);
            campanha.ProviderIa = generated.Provider;
            campanha.ModeloIa = generated.Modelo;
            campanha.DuracaoGeracaoMs = generated.DuracaoMs;
            campanha.DataGeracao = DateTime.UtcNow;
            campanha.Status = StatusCampanha.Gerada;
            campanha.ErroGeracao = null;
            campanha.DataAtualizacao = DateTime.UtcNow;
            await repository.SalvarAsync(finalizationToken);
            return CampanhaMapping.ToResponse(campanha);
        }
        catch (Exception ex)
        {
            campanha.Status = StatusCampanha.Erro;
            campanha.ErroGeracao = SafeGenerationError(ex);
            campanha.DataAtualizacao = DateTime.UtcNow;
            await repository.SalvarAsync(finalizationToken);
            throw new CampaignGenerationException("Nao foi possivel gerar a campanha. Verifique a configuracao do provedor de IA.", ex);
        }
    }

    public async Task<IReadOnlyList<CampanhaResponse>> ListarCampanhasAsync(CancellationToken cancellationToken)
    {
        var campanhas = await repository.ListarAsync(cancellationToken);
        return campanhas.Select(CampanhaMapping.ToResponse).ToArray();
    }

    public async Task<CampanhaResponse?> ObterCampanhaPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var campanha = await repository.ObterPorIdAsync(id, cancellationToken);
        return campanha is null ? null : CampanhaMapping.ToResponse(campanha);
    }

    private async Task<string> EnsureUniqueSlugAsync(string value, Guid? ignorarId, CancellationToken cancellationToken)
    {
        var baseSlug = CampanhaText.Slugify(value);
        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            baseSlug = $"campanha-{Guid.NewGuid():N}"[..17];
        }

        if (baseSlug.Length > 180)
        {
            baseSlug = baseSlug[..180].Trim('-');
        }

        var candidate = baseSlug;
        var suffix = 2;
        while (await repository.ExisteSlugAsync(candidate, ignorarId, cancellationToken))
        {
            var ending = $"-{suffix++}";
            var maxBaseLength = 180 - ending.Length;
            candidate = $"{baseSlug[..Math.Min(baseSlug.Length, maxBaseLength)].Trim('-')}{ending}";
        }

        return candidate;
    }

    private static string Serialize<T>(T items)
    {
        return JsonSerializer.Serialize(items);
    }

    private static string SafeText(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string SafeGenerationError(Exception ex)
    {
        var message = ex is CampaignGenerationException or InvalidOperationException or ArgumentException
            ? ex.Message
            : "Falha inesperada durante a geracao da campanha.";

        return CampanhaText.Limitar(message, 500) ?? "Falha inesperada durante a geracao da campanha.";
    }

    private async Task<Segment?> ResolveSegmentAsync(string? segmentSlug, CancellationToken cancellationToken)
    {
        if (segmentRepository is null)
        {
            return null;
        }

        var slug = string.IsNullOrWhiteSpace(segmentSlug)
            ? DefaultSegmentSlug
            : CampanhaText.Slugify(segmentSlug);

        var segment = await segmentRepository.GetBySlugAsync(slug, cancellationToken);
        if (segment is null)
        {
            throw new ArgumentException($"Segmento '{slug}' nao encontrado ou inativo.");
        }

        if (!segment.IsActive)
        {
            throw new ArgumentException($"Segmento '{slug}' nao encontrado ou inativo.");
        }

        return segment;
    }

    private static string? BuildCampaignConfigJson(GerarCampanhaRequest request)
    {
        var config = new CampaignContextConfig(
            CampanhaText.Limitar(request.BusinessDescription, 500),
            CampanhaText.Limitar(request.ProductOrService, 180),
            CampanhaText.Limitar(request.TargetAudience, 300),
            CampanhaText.Limitar(request.CampaignGoal, 300),
            CampanhaText.Limitar(request.Offer, 300),
            NormalizeLocation(request.Location),
            CampanhaText.Limitar(request.BrandTone, 120),
            NormalizeRestrictions(request.Restrictions));

        if (!config.HasValues)
        {
            return null;
        }

        return JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private static CampaignLocationDto? NormalizeLocation(CampaignLocationDto? location)
    {
        if (location is null)
        {
            return null;
        }

        var normalized = new CampaignLocationDto(
            CampanhaText.Limitar(location.City, 120),
            CampanhaText.Limitar(location.State, 2)?.ToUpperInvariant(),
            CampanhaText.Limitar(location.Region, 120));

        return string.IsNullOrWhiteSpace(normalized.City)
            && string.IsNullOrWhiteSpace(normalized.State)
            && string.IsNullOrWhiteSpace(normalized.Region)
            ? null
            : normalized;
    }

    private static IReadOnlyCollection<string>? NormalizeRestrictions(IReadOnlyCollection<string>? restrictions)
    {
        var items = (restrictions ?? [])
            .Select(x => CampanhaText.Limitar(x, 180))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return items.Length == 0 ? null : items;
    }

    private sealed record CampaignContextConfig(
        string? BusinessDescription,
        string? ProductOrService,
        string? TargetAudience,
        string? CampaignGoal,
        string? Offer,
        CampaignLocationDto? Location,
        string? BrandTone,
        IReadOnlyCollection<string>? Restrictions)
    {
        public bool HasValues =>
            !string.IsNullOrWhiteSpace(BusinessDescription)
            || !string.IsNullOrWhiteSpace(ProductOrService)
            || !string.IsNullOrWhiteSpace(TargetAudience)
            || !string.IsNullOrWhiteSpace(CampaignGoal)
            || !string.IsNullOrWhiteSpace(Offer)
            || Location is not null
            || !string.IsNullOrWhiteSpace(BrandTone)
            || Restrictions is { Count: > 0 };
    }
}
