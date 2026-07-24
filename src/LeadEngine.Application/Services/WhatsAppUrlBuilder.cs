using System.Text;
using LeadEngine.Application.Common;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using Microsoft.Extensions.Options;

namespace LeadEngine.Application.Services;

public sealed class WhatsAppUrlBuilder(IOptions<WhatsAppOptions> options) : IWhatsAppUrlBuilder
{
    public string Build(Lead lead, Campanha campanha)
    {
        var numero = LeadSanitizer.Digitos(options.Value.Numero);
        var mensagemPadrao = string.IsNullOrWhiteSpace(options.Value.MensagemPadrao)
            ? "Gostaria de receber uma cotacao."
            : options.Value.MensagemPadrao.Trim();

        var message = new StringBuilder();
        message.AppendLine($"Ola, meu nome e {lead.Nome}.");
        message.AppendLine($"Tenho interesse na campanha {campanha.Nome}.");
        message.AppendLine();
        message.AppendLine($"Cidade: {lead.Cidade}/{lead.Uf}");
        message.AppendLine($"Quantidade de vidas: {lead.QuantidadeVidas}");
        message.AppendLine($"Tipo de contratacao: {lead.TipoContratacao}");
        if (!string.IsNullOrWhiteSpace(lead.Observacao))
        {
            message.AppendLine();
            message.AppendLine($"Observacao: {lead.Observacao}");
        }

        message.AppendLine();
        message.AppendLine(mensagemPadrao);

        return $"https://wa.me/{numero}?text={Uri.EscapeDataString(message.ToString().Trim())}";
    }
}
