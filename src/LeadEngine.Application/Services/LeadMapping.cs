using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Services;

public static class LeadMapping
{
    public static LeadResponse ToResponse(Lead lead)
    {
        return new LeadResponse(
            lead.Id,
            lead.CampanhaId,
            lead.Campanha?.Nome,
            lead.Tipo,
            lead.TipoContratacao,
            lead.Status,
            lead.Nome,
            LeadSanitizer.MascaraTelefone(lead.WhatsApp),
            LeadSanitizer.MascaraEmail(lead.Email),
            lead.Cidade,
            lead.Uf,
            lead.QuantidadeVidas,
            lead.OrigemCaptura,
            lead.Origem?.LandingPage,
            lead.Origem?.UtmCampaign,
            lead.CriadoEm,
            lead.EnviadoEm,
            lead.ErroEnvio,
            lead.StatusEnvioExterno);
    }

    public static LeadDetalheResponse ToDetalhe(Lead lead)
    {
        return new LeadDetalheResponse
        {
            Id = lead.Id,
            CampanhaId = lead.CampanhaId,
            CampanhaNome = lead.Campanha?.Nome,
            Tipo = lead.Tipo,
            TipoContratacao = lead.TipoContratacao,
            Status = lead.Status,
            Nome = lead.Nome,
            WhatsAppMascarado = LeadSanitizer.MascaraTelefone(lead.WhatsApp),
            EmailMascarado = LeadSanitizer.MascaraEmail(lead.Email),
            CepMascarado = LeadSanitizer.MascaraDocumento(lead.Cep),
            Cidade = lead.Cidade,
            Uf = lead.Uf,
            QuantidadeVidas = lead.QuantidadeVidas,
            Observacao = lead.Observacao,
            IdadesJson = lead.IdadesJson,
            HospitalDesejado = lead.HospitalDesejado,
            OperadoraDesejada = lead.OperadoraDesejada,
            PossuiPlanoAtual = lead.PossuiPlanoAtual,
            PlanoAtual = lead.PlanoAtual,
            NomeEmpresa = lead.NomeEmpresa,
            CnpjMascarado = LeadSanitizer.MascaraDocumento(lead.Cnpj),
            QuantidadeFuncionarios = lead.QuantidadeFuncionarios,
            ConsentimentoContato = lead.ConsentimentoContato,
            ConsentimentoEm = lead.ConsentimentoEm,
            CriadoEm = lead.CriadoEm,
            OrigemCaptura = lead.OrigemCaptura,
            UtmSource = lead.UtmSource,
            UtmMedium = lead.UtmMedium,
            UtmCampaign = lead.UtmCampaign,
            UtmTerm = lead.UtmTerm,
            UtmContent = lead.UtmContent,
            Gclid = lead.Gclid,
            Fbclid = lead.Fbclid,
            StatusEnvioExterno = lead.StatusEnvioExterno,
            TentativasEnvioExterno = lead.TentativasEnvioExterno,
            UltimoErroEnvioExterno = lead.UltimoErroEnvioExterno,
            EnviadoEm = lead.EnviadoEm,
            ErroEnvio = lead.ErroEnvio,
            Origem = lead.Origem is null ? null : new OrigemLeadDto(
                lead.Origem.Gclid,
                lead.Origem.Gbraid,
                lead.Origem.Wbraid,
                lead.Origem.UtmSource,
                lead.Origem.UtmMedium,
                lead.Origem.UtmCampaign,
                lead.Origem.UtmContent,
                lead.Origem.UtmTerm,
                lead.Origem.CampaignId,
                lead.Origem.AdGroupId,
                lead.Origem.AdId,
                lead.Origem.Keyword,
                lead.Origem.MatchType,
                lead.Origem.Device,
                lead.Origem.LandingPage,
                lead.Origem.Referrer,
                lead.Origem.UserAgent),
            LogsIntegracao = lead.LogsIntegracao
                .OrderByDescending(l => l.CriadoEm)
                .Select(l => new LogIntegracaoLeadDto(l.CriadoEm, l.Sucesso, l.StatusHttp, l.Mensagem, l.Endpoint))
                .ToArray()
        };
    }
}
