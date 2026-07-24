using System.Text;
using LeadEngine.Application.Common;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;
using Microsoft.Extensions.Options;

namespace LeadEngine.Application.Services;

public sealed class WhatsAppUrlBuilder(IOptions<WhatsAppOptions> options, IConfigurationResolver? resolver = null) : IWhatsAppUrlBuilder
{
    public string Build(Lead lead, Campanha campanha)
    {
        var numeroConfig = Resolve("Numero") ?? options.Value.Numero;
        var mensagemConfig = Resolve("MensagemPadrao") ?? options.Value.MensagemPadrao;
        var numero = LeadSanitizer.Digitos(numeroConfig);
        var mensagemPadrao = string.IsNullOrWhiteSpace(mensagemConfig)
            ? "Gostaria de receber uma cotacao."
            : mensagemConfig.Trim();

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

    private string? Resolve(string key)
    {
        return resolver?.ResolveAsync(CategoriaConfiguracao.WhatsApp, key, CancellationToken.None).GetAwaiter().GetResult().Value;
    }
}
