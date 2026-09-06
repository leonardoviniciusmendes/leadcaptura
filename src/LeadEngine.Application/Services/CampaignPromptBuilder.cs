using System.Globalization;
using System.Text.Json;
using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Services;

public sealed class CampaignPromptBuilder
{
    public string Build(CampaignGenerationContext context)
    {
        var budget = context.LegacyContext.OrcamentoDiario.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));

        return $$"""
        Voce e um especialista em campanhas digitais de aquisicao de leads.
        Gere uma campanha de captacao qualificada usando exclusivamente o contexto fornecido.

        Contexto do segmento:
        - Segmento: {{ValueOrDefault(context.SegmentName)}}
        - Slug do segmento: {{ValueOrDefault(context.SegmentSlug)}}
        - TemplateKey: {{ValueOrDefault(context.TemplateKey)}}
        - Defaults configurados do segmento: {{JsonOrDefault(context.SegmentDefaultConfig)}}

        Contexto da campanha:
        - Descricao do negocio: {{ValueOrDefault(context.BusinessDescription)}}
        - Produto ou servico: {{ValueOrDefault(context.ProductOrService)}}
        - Publico-alvo: {{ValueOrDefault(context.TargetAudience)}}
        - Objetivo da campanha: {{ValueOrDefault(context.CampaignGoal)}}
        - Oferta: {{ValueOrDefault(context.Offer)}}
        - Localizacao: {{JsonSerializer.Serialize(context.Location)}}
        - Tom de marca: {{ValueOrDefault(context.BrandTone)}}
        - Restricoes da campanha: {{JsonSerializer.Serialize(context.Restrictions ?? [])}}
        - Configuracao adicional da campanha: {{JsonOrDefault(context.CampaignConfigJson)}}

        Contexto legado temporario:
        - Tipo de publico: {{context.LegacyContext.TipoPublico}}
        - Cidade: {{ValueOrDefault(context.LegacyContext.Cidade)}}
        - Estado: {{ValueOrDefault(context.LegacyContext.Estado)}}
        - Bairro ou regiao: {{ValueOrDefault(context.LegacyContext.Regiao)}}
        - Operadora/fornecedor legado: {{ValueOrDefault(context.LegacyContext.Operadora)}}
        - Outro fornecedor legado: {{ValueOrDefault(context.LegacyContext.OperadoraOutra)}}
        - Orcamento diario: {{budget}}
        - Objetivo ou observacao legado: {{ValueOrDefault(context.LegacyContext.Objetivo)}}

        Regras obrigatorias:
        - Responda exclusivamente em JSON valido.
        - Use portugues do Brasil.
        - Use linguagem clara, profissional e objetiva.
        - Nao invente fatos sobre o negocio, produto, servico, disponibilidade, preco, condicoes, certificacoes ou resultados.
        - Nao faca promessas enganosas ou garantias absolutas.
        - Respeite as restricoes informadas no contexto do segmento e da campanha.
        - Use o TemplateKey apenas para orientar o formato comercial da oferta, nunca como segmento.
        - Palavras-chave devem ter intencao comercial e evitar termos excessivamente amplos.
        - Titulos de anuncios: gerar entre 8 e 12, cada um com no maximo 30 caracteres.
        - Descricoes: gerar entre 3 e 4, cada uma com no maximo 90 caracteres.
        - Beneficios: gerar entre 3 e 6, sem promessas garantidas.
        - FAQ: gerar entre 3 e 6 perguntas e respostas.
        - Palavras negativas devem incluir termos de baixa intencao comercial quando fizer sentido para o contexto.

        Formato exato esperado:
        {
          "nome": "string",
          "slug": "string",
          "tituloLandingPage": "string",
          "subtituloLandingPage": "string",
          "textoBotao": "string",
          "mensagemWhatsApp": "string",
          "beneficios": ["string"],
          "perguntasFrequentes": [
            { "pergunta": "string", "resposta": "string" }
          ],
          "palavrasChave": ["string"],
          "palavrasChaveNegativas": ["string"],
          "titulosAnuncios": ["string"],
          "descricoesAnuncios": ["string"]
        }
        """;
    }

    private static string ValueOrDefault(string? value) => string.IsNullOrWhiteSpace(value) ? "nao informado" : value;

    private static string JsonOrDefault(string? value) => string.IsNullOrWhiteSpace(value) ? "{}" : value;
}
