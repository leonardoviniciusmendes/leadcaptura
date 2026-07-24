using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Services;

public sealed class CampanhaService(
    ICampanhaRepository repository,
    ICampaignGenerationService generationService)
{
    public async Task<CampanhaResponse> GerarCampanhaAsync(GerarCampanhaRequest request, CancellationToken cancellationToken)
    {
        CampanhaValidator.ValidarBriefing(request);

        var generated = generationService.Generate(request);
        var now = DateTime.UtcNow;
        var campanha = new Campanha
        {
            Id = Guid.NewGuid(),
            Nome = generated.Nome,
            TipoPublico = request.TipoPublico,
            Cidade = request.Cidade.Trim(),
            Estado = request.Estado.Trim().ToUpperInvariant(),
            Regiao = CampanhaText.Limitar(request.Regiao, 120),
            Operadora = CampanhaValidator.OperadoraEfetiva(request),
            OrcamentoDiario = request.OrcamentoDiario,
            Objetivo = CampanhaText.Limitar(request.Objetivo, 500),
            Status = StatusCampanha.Gerada,
            TituloLandingPage = generated.TituloLandingPage,
            SubtituloLandingPage = generated.SubtituloLandingPage,
            TextoBotao = generated.TextoBotao,
            MensagemWhatsApp = generated.MensagemWhatsApp,
            Slug = await EnsureUniqueSlugAsync(generated.Slug, null, cancellationToken),
            DataCriacao = now
        };

        await repository.AdicionarAsync(campanha, cancellationToken);
        await repository.SalvarAsync(cancellationToken);
        return CampanhaMapping.ToResponse(campanha);
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

    public async Task<CampanhaResponse> RevisarCampanhaAsync(Guid id, RevisarCampanhaRequest request, CancellationToken cancellationToken)
    {
        CampanhaValidator.ValidarRevisao(request);

        var campanha = await repository.ObterPorIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Campanha não encontrada.");

        campanha.Nome = request.Nome.Trim();
        campanha.TituloLandingPage = request.TituloLandingPage.Trim();
        campanha.SubtituloLandingPage = request.SubtituloLandingPage.Trim();
        campanha.TextoBotao = request.TextoBotao.Trim();
        campanha.MensagemWhatsApp = request.MensagemWhatsApp.Trim();
        campanha.Slug = await EnsureUniqueSlugAsync(request.Slug, campanha.Id, cancellationToken);
        campanha.Objetivo = CampanhaText.Limitar(request.Objetivo, 500);
        campanha.Status = request.Status;
        campanha.DataAtualizacao = DateTime.UtcNow;

        await repository.SalvarAsync(cancellationToken);
        return CampanhaMapping.ToResponse(campanha);
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
}
