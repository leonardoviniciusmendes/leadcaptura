using System.Text.Json;
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

        var now = DateTime.UtcNow;
        var campanha = new Campanha
        {
            Id = Guid.NewGuid(),
            Nome = "Campanha em geracao",
            TipoPublico = request.TipoPublico,
            Cidade = request.Cidade.Trim(),
            Estado = request.Estado.Trim().ToUpperInvariant(),
            Regiao = CampanhaText.Limitar(request.Regiao, 120),
            Operadora = CampanhaValidator.OperadoraEfetiva(request),
            OrcamentoDiario = request.OrcamentoDiario,
            Objetivo = CampanhaText.Limitar(request.Objetivo, 500),
            Status = StatusCampanha.Gerando,
            Slug = $"campanha-{Guid.NewGuid():N}"[..17],
            DataCriacao = now
        };

        await repository.AdicionarAsync(campanha, cancellationToken);
        await repository.SalvarAsync(cancellationToken);

        var finalizationToken = CancellationToken.None;
        try
        {
            var generated = await generationService.GenerateAsync(request, finalizationToken);
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

    private static string SafeGenerationError(Exception ex)
    {
        var message = ex is CampaignGenerationException or InvalidOperationException or ArgumentException
            ? ex.Message
            : "Falha inesperada durante a geracao da campanha.";

        return CampanhaText.Limitar(message, 500) ?? "Falha inesperada durante a geracao da campanha.";
    }
}
