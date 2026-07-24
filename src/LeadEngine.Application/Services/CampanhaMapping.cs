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
            campanha.DataCriacao,
            campanha.DataAtualizacao);
    }
}
