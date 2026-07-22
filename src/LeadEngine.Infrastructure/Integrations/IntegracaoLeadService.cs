using System.Net.Http.Json;
using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using Microsoft.Extensions.Options;

namespace LeadEngine.Infrastructure.Integrations;

public sealed class IntegracaoLeadService(HttpClient httpClient, IOptions<IntegracaoLeadsOptions> options)
    : IIntegracaoLeadService
{
    public async Task<ResultadoIntegracaoLead> EnviarAsync(Lead lead, CancellationToken cancellationToken)
    {
        var config = options.Value;
        if (string.IsNullOrWhiteSpace(config.BaseUrl))
        {
            return new ResultadoIntegracaoLead(false, null, "Integracao externa nao configurada.", config.Endpoint);
        }

        var payload = new
        {
            lead.Id,
            Tipo = lead.Tipo.ToString(),
            lead.Nome,
            WhatsApp = lead.WhatsAppNormalizado,
            lead.Email,
            lead.Cep,
            lead.Cidade,
            lead.Uf,
            lead.QuantidadeVidas,
            lead.IdadesJson,
            lead.HospitalDesejado,
            lead.OperadoraDesejada,
            lead.PossuiPlanoAtual,
            lead.PlanoAtual,
            lead.NomeEmpresa,
            Cnpj = lead.CnpjNormalizado,
            lead.QuantidadeFuncionarios,
            lead.ConsentimentoContato,
            lead.ConsentimentoEm,
            lead.TextoConsentimentoVersao,
            Origem = lead.Origem
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, config.Endpoint)
            {
                Content = JsonContent.Create(payload)
            };

            if (!string.IsNullOrWhiteSpace(config.ApiKey))
            {
                request.Headers.Add("X-Api-Key", config.ApiKey);
            }

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var message = response.IsSuccessStatusCode
                ? "Lead enviado com sucesso."
                : LeadSanitizer.Texto(body, 500) ?? "Falha no envio externo.";

            return new ResultadoIntegracaoLead(response.IsSuccessStatusCode, (int)response.StatusCode, message, config.Endpoint);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new ResultadoIntegracaoLead(false, null, ex.Message, config.Endpoint);
        }
    }
}
