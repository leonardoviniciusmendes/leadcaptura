using System.Text.Json;
using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Services;

public sealed class LeadCaptureService(
    ILeadRepository repository,
    IIntegracaoLeadService integracaoLeadService,
    IRequestContext requestContext)
{
    private const string ConsentimentoVersao = "lead-engine-contato-v1";

    public async Task<CapturaLeadResponse> CapturarAsync(CriarLeadRequest request, CancellationToken cancellationToken)
    {
        var erros = LeadValidator.Validar(request);
        if (erros.Count > 0)
        {
            throw new ArgumentException(string.Join(" ", erros));
        }

        var whatsAppNormalizado = LeadSanitizer.Digitos(request.WhatsApp);
        var origem = request.Origem;
        var duplicado = await repository.ObterDuplicadoRecenteAsync(
            whatsAppNormalizado,
            DateTime.UtcNow.AddHours(-24),
            cancellationToken);

        if (duplicado is not null)
        {
            await repository.AdicionarTentativaAsync(new TentativaCapturaLead
            {
                Id = Guid.NewGuid(),
                LeadId = duplicado.Id,
                WhatsAppNormalizado = whatsAppNormalizado,
                Duplicado = true,
                CriadoEm = DateTime.UtcNow,
                LandingPage = LeadSanitizer.Texto(origem?.LandingPage, 250),
                UtmCampaign = LeadSanitizer.Texto(origem?.UtmCampaign, 180),
                Gclid = LeadSanitizer.Texto(origem?.Gclid, 180)
            }, cancellationToken);
            await repository.SalvarAsync(cancellationToken);

            return new CapturaLeadResponse(true, duplicado.Id, "Sua solicitacao ja foi recebida.", true);
        }

        var lead = CriarLead(request, whatsAppNormalizado);
        await repository.AdicionarAsync(lead, cancellationToken);
        await repository.AdicionarTentativaAsync(new TentativaCapturaLead
        {
            Id = Guid.NewGuid(),
            LeadId = lead.Id,
            WhatsAppNormalizado = lead.WhatsAppNormalizado,
            Duplicado = false,
            CriadoEm = lead.CriadoEm,
            LandingPage = lead.Origem.LandingPage,
            UtmCampaign = lead.Origem.UtmCampaign,
            Gclid = lead.Origem.Gclid
        }, cancellationToken);
        await repository.SalvarAsync(cancellationToken);

        await EnviarLeadAsync(lead, cancellationToken);
        return new CapturaLeadResponse(true, lead.Id, "Solicitacao recebida com sucesso.");
    }

    public async Task<CapturaLeadResponse> ReenviarAsync(Guid id, CancellationToken cancellationToken)
    {
        var lead = await repository.ObterPorIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Lead nao encontrado.");

        await EnviarLeadAsync(lead, cancellationToken);
        return new CapturaLeadResponse(true, lead.Id, "Reenvio processado.");
    }

    private async Task EnviarLeadAsync(Lead lead, CancellationToken cancellationToken)
    {
        var resultado = await integracaoLeadService.EnviarAsync(lead, cancellationToken);
        lead.Status = resultado.Sucesso ? StatusLead.Enviado : StatusLead.ErroEnvio;
        lead.EnviadoEm = resultado.Sucesso ? DateTime.UtcNow : null;
        lead.ErroEnvio = resultado.Sucesso ? null : LeadSanitizer.Texto(resultado.Mensagem, 500);
        lead.UltimoStatusHttpIntegracao = resultado.StatusHttp;

        await repository.AdicionarLogIntegracaoAsync(new LogIntegracaoLead
        {
            Id = Guid.NewGuid(),
            LeadId = lead.Id,
            CriadoEm = DateTime.UtcNow,
            Sucesso = resultado.Sucesso,
            StatusHttp = resultado.StatusHttp,
            Mensagem = LeadSanitizer.Texto(resultado.Mensagem, 500),
            Endpoint = LeadSanitizer.Texto(resultado.Endpoint, 250)
        }, cancellationToken);
        await repository.SalvarAsync(cancellationToken);
    }

    private Lead CriarLead(CriarLeadRequest request, string whatsAppNormalizado)
    {
        var now = DateTime.UtcNow;
        var cep = LeadSanitizer.Digitos(request.Cep);
        var cnpj = LeadSanitizer.Digitos(request.Cnpj);
        var email = LeadSanitizer.Email(request.Email);
        var origem = request.Origem;

        return new Lead
        {
            Id = Guid.NewGuid(),
            Tipo = request.Tipo,
            Nome = LeadSanitizer.Texto(request.Nome, 150)!,
            WhatsApp = whatsAppNormalizado,
            WhatsAppNormalizado = whatsAppNormalizado,
            Email = email,
            EmailNormalizado = email,
            Cep = string.IsNullOrWhiteSpace(cep) ? null : cep,
            Cidade = LeadSanitizer.Texto(request.Cidade, 120),
            Uf = LeadSanitizer.Texto(request.Uf?.ToUpperInvariant(), 2),
            QuantidadeVidas = request.QuantidadeVidas,
            IdadesJson = request.Idades is null ? null : JsonSerializer.Serialize(request.Idades),
            HospitalDesejado = LeadSanitizer.Texto(request.HospitalDesejado, 120),
            OperadoraDesejada = LeadSanitizer.Texto(request.OperadoraDesejada, 120),
            PossuiPlanoAtual = request.PossuiPlanoAtual,
            PlanoAtual = LeadSanitizer.Texto(request.PlanoAtual, 120),
            NomeEmpresa = LeadSanitizer.Texto(request.NomeEmpresa, 180),
            Cnpj = string.IsNullOrWhiteSpace(cnpj) ? null : cnpj,
            CnpjNormalizado = string.IsNullOrWhiteSpace(cnpj) ? null : cnpj,
            QuantidadeFuncionarios = request.QuantidadeFuncionarios,
            Status = StatusLead.Validado,
            ConsentimentoContato = true,
            ConsentimentoEm = now,
            TextoConsentimentoVersao = ConsentimentoVersao,
            CriadoEm = now,
            Origem = new OrigemLead
            {
                Id = Guid.NewGuid(),
                Gclid = LeadSanitizer.Texto(origem?.Gclid, 180),
                Gbraid = LeadSanitizer.Texto(origem?.Gbraid, 180),
                Wbraid = LeadSanitizer.Texto(origem?.Wbraid, 180),
                UtmSource = LeadSanitizer.Texto(origem?.UtmSource, 100),
                UtmMedium = LeadSanitizer.Texto(origem?.UtmMedium, 100),
                UtmCampaign = LeadSanitizer.Texto(origem?.UtmCampaign, 180),
                UtmContent = LeadSanitizer.Texto(origem?.UtmContent, 180),
                UtmTerm = LeadSanitizer.Texto(origem?.UtmTerm, 180),
                CampaignId = LeadSanitizer.Texto(origem?.CampaignId, 80),
                AdGroupId = LeadSanitizer.Texto(origem?.AdGroupId, 80),
                AdId = LeadSanitizer.Texto(origem?.AdId, 80),
                Keyword = LeadSanitizer.Texto(origem?.Keyword, 180),
                MatchType = LeadSanitizer.Texto(origem?.MatchType, 80),
                Device = LeadSanitizer.Texto(origem?.Device, 80),
                LandingPage = LeadSanitizer.Texto(origem?.LandingPage, 250),
                Referrer = LeadSanitizer.Texto(origem?.Referrer, 500),
                UserAgent = LeadSanitizer.Texto(origem?.UserAgent ?? requestContext.UserAgent, 500),
                IpHash = requestContext.IpHash
            }
        };
    }
}
