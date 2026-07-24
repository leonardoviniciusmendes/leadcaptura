using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Services;

public sealed class LeadAttributionService(
    ILeadRepository leadRepository,
    IGoogleAdsPublicationRepository publicationRepository) : ILeadAttributionService
{
    public async Task<int> AtribuirAsync(Guid? publicacaoId, CancellationToken cancellationToken)
    {
        var publications = publicacaoId is null
            ? await publicationRepository.ListarAsync(new GoogleAdsPublicationQuery(null, null, null, null, null), cancellationToken)
            : [await publicationRepository.ObterPorIdAsync(publicacaoId.Value, cancellationToken) ?? throw new KeyNotFoundException("Publicacao Google Ads nao encontrada.")];
        var changed = 0;
        foreach (var publication in publications)
        {
            var leads = await leadRepository.ListarPorCampanhaAsync(publication.CampanhaId, new LeadQuery(publication.CampanhaId, null, null, null, null, null, null, null, null, null, 1, 100), cancellationToken);
            foreach (var lead in leads)
            {
                var type = Resolve(lead);
                if ((int)type <= (int)lead.TipoAtribuicao)
                {
                    continue;
                }

                lead.TipoAtribuicao = type;
                lead.GoogleAdsPublicacaoId = publication.Id;
                lead.DataAtribuicao = DateTime.UtcNow;
                changed++;
            }
        }
        await leadRepository.SalvarAsync(cancellationToken);
        return changed;
    }

    private static TipoAtribuicaoLead Resolve(Domain.Entities.Lead lead)
    {
        if (lead.CampanhaId is not null) return TipoAtribuicaoLead.Direta;
        if (!string.IsNullOrWhiteSpace(lead.Gclid)) return TipoAtribuicaoLead.Gclid;
        if (!string.IsNullOrWhiteSpace(lead.UtmCampaign)) return TipoAtribuicaoLead.Utm;
        if (!string.IsNullOrWhiteSpace(lead.Origem?.LandingPage)) return TipoAtribuicaoLead.Landing;
        return TipoAtribuicaoLead.NaoAtribuida;
    }
}
