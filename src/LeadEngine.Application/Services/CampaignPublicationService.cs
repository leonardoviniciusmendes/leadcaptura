using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Services;

public sealed class CampaignPublicationService(ICampanhaRepository repository, IConfigurationResolver? resolver = null) : ICampaignPublicationService
{
    public async Task<CampanhaPublicacaoResponse> PublicarAsync(Guid id, CancellationToken cancellationToken)
    {
        var campanha = await ObterAsync(id, cancellationToken);
        if (campanha.Status is StatusCampanha.Gerando or StatusCampanha.Erro)
        {
            throw new ArgumentException("Campanha com status Gerando ou Erro nao pode ser publicada.");
        }

        if (campanha.Status != StatusCampanha.Revisada)
        {
            throw new ArgumentException("Somente campanhas revisadas podem ser publicadas.");
        }

        ValidateCurrent(campanha);
        campanha.Publicada = true;
        campanha.Ativo = true;
        campanha.DataPublicacao ??= DateTime.UtcNow;
        campanha.DataDespublicacao = null;
        campanha.UrlPublica = await PublicUrlAsync(campanha.Slug, cancellationToken);
        await repository.SalvarAsync(cancellationToken);
        return ToResponse(campanha);
    }

    public async Task<CampanhaPublicacaoResponse> DespublicarAsync(Guid id, CancellationToken cancellationToken)
    {
        var campanha = await ObterAsync(id, cancellationToken);
        campanha.Publicada = false;
        campanha.Ativo = false;
        campanha.DataDespublicacao = DateTime.UtcNow;
        await repository.SalvarAsync(cancellationToken);
        return ToResponse(campanha);
    }

    public async Task<CampanhaPublicacaoResponse> ObterPublicacaoAsync(Guid id, CancellationToken cancellationToken)
    {
        return ToResponse(await ObterAsync(id, cancellationToken));
    }

    private async Task<Campanha> ObterAsync(Guid id, CancellationToken cancellationToken)
    {
        return await repository.ObterPorIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Campanha nao encontrada.");
    }

    private static void ValidateCurrent(Campanha campanha)
    {
        var atual = CampanhaContentSnapshot.From(campanha);
        CampanhaValidator.ValidarCampanhaCompleta(new CampanhaConteudoNormalizado(
            atual.TituloLandingPage,
            atual.SubtituloLandingPage,
            atual.TextoBotao,
            atual.MensagemWhatsApp,
            atual.Beneficios,
            atual.PerguntasFrequentes.Select(x => new FaqItemValidation(x.Pergunta, x.Resposta)).ToArray(),
            atual.PalavrasChave,
            atual.PalavrasChaveNegativas,
            atual.TitulosAnuncios,
            atual.DescricoesAnuncios));
    }

    private static CampanhaPublicacaoResponse ToResponse(Campanha campanha)
    {
        return new CampanhaPublicacaoResponse(
            campanha.Id,
            campanha.Status,
            campanha.Publicada,
            campanha.Ativo,
            campanha.DataPublicacao,
            campanha.DataDespublicacao,
            campanha.Slug,
            campanha.UrlPublica);
    }

    private async Task<string> PublicUrlAsync(string slug, CancellationToken cancellationToken)
    {
        if (resolver is null)
        {
            return $"/lp/{slug}";
        }

        return await new CampaignPublicUrlBuilder(resolver).BuildRequiredAsync(slug, null, cancellationToken);
    }
}
