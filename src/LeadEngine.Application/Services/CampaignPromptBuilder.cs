using System.Globalization;
using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Services;

public sealed class CampaignPromptBuilder
{
    public string Build(GerarCampanhaRequest briefing)
    {
        var operadora = CampanhaValidator.OperadoraEfetiva(briefing);
        var budget = briefing.OrcamentoDiario.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));

        return $$"""
        Você é um especialista em campanhas de Google Ads para planos de saúde no Brasil.
        Gere uma campanha de captação qualificada para um corretor de planos de saúde.

        Dados do briefing:
        - Tipo de público: {{briefing.TipoPublico}}
        - Cidade: {{briefing.Cidade}}
        - Estado: {{briefing.Estado}}
        - Bairro ou região: {{briefing.Regiao ?? "não informado"}}
        - Operadora: {{operadora}}
        - Orçamento diário: {{budget}}
        - Objetivo ou observação: {{briefing.Objetivo ?? "não informado"}}

        Regras obrigatórias:
        - Responda exclusivamente em JSON válido.
        - Use português do Brasil.
        - Use linguagem clara, profissional e objetiva.
        - Não faça promessas enganosas.
        - Não garanta preço, economia, cobertura, aprovação, contratação ou ausência de carência.
        - Não invente informações sobre operadoras, rede credenciada, hospitais ou coberturas.
        - Palavras-chave devem ter intenção comercial e evitar termos excessivamente amplos.
        - Títulos de anúncios: gerar entre 8 e 12, cada um com no máximo 30 caracteres.
        - Descrições: gerar entre 3 e 4, cada uma com no máximo 90 caracteres.
        - Benefícios: gerar entre 3 e 6, sem promessas de menor preço, aprovação garantida, cobertura garantida ou carência zero.
        - FAQ: gerar entre 3 e 6 perguntas e respostas.
        - Respostas do FAQ devem deixar claro que preço varia por idade, região e contratação; rede e cobertura dependem do plano; carência depende da operadora; disponibilidade depende de análise comercial.
        - Palavras negativas devem incluir termos de baixa intenção comercial como emprego, salário, concurso, segunda via, telefone da operadora, reclamação, login, boleto e cancelamento, além de outras relevantes.

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
}
