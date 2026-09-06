using System.Text.Json;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Services;

public sealed class CampaignSectionPromptBuilder
{
    public string Build(Campanha campanha, CampanhaSecao secao, string? instrucaoAdicional)
    {
        var atual = CampanhaContentSnapshot.From(campanha);
        var context = CampaignGenerationContextFactory.FromCampaign(campanha);
        var formato = secao switch
        {
            CampanhaSecao.Nome => """{ "nome": "string" }""",
            CampanhaSecao.LandingPage => """{ "tituloLandingPage": "string", "subtituloLandingPage": "string", "textoBotao": "string" }""",
            CampanhaSecao.MensagemWhatsApp => """{ "mensagemWhatsApp": "string" }""",
            CampanhaSecao.Beneficios => """{ "beneficios": ["string"] }""",
            CampanhaSecao.PerguntasFrequentes => """{ "perguntasFrequentes": [{ "pergunta": "string", "resposta": "string" }] }""",
            CampanhaSecao.PalavrasChave => """{ "palavrasChave": ["string"] }""",
            CampanhaSecao.PalavrasChaveNegativas => """{ "palavrasChaveNegativas": ["string"] }""",
            CampanhaSecao.TitulosAnuncios => """{ "titulosAnuncios": ["string"] }""",
            CampanhaSecao.DescricoesAnuncios => """{ "descricoesAnuncios": ["string"] }""",
            _ => throw new ArgumentException("Secao invalida.")
        };

        return $$"""
        Voce e um especialista em campanhas digitais de aquisicao de leads.
        Regenere somente a secao solicitada da campanha. Nao gere a campanha inteira.

        Contexto de geracao:
        {{JsonSerializer.Serialize(context)}}

        Conteudo atual da campanha:
        {{JsonSerializer.Serialize(atual)}}

        Secao a regenerar: {{secao}}
        Instrucao adicional: {{instrucaoAdicional ?? "nao informada"}}

        Regras:
        - Use portugues do Brasil.
        - Seja claro, profissional e objetivo.
        - Nao invente fatos sobre o negocio, produto, servico, disponibilidade, preco, condicoes, certificacoes ou resultados.
        - Nao faca promessas enganosas ou garantias absolutas.
        - Respeite as restricoes presentes no contexto.
        - Palavras-chave devem ter intencao comercial e evitar termos excessivamente amplos.
        - Titulos de anuncios: entre 8 e 12, maximo 30 caracteres cada, sem duplicatas exatas.
        - Descricoes: entre 3 e 4, maximo 90 caracteres cada, sem duplicatas exatas.
        - Beneficios: entre 3 e 6.
        - FAQ: entre 3 e 6 perguntas e respostas obrigatorias.
        - Palavras-chave: pelo menos 3, sem duplicatas.
        - Palavras negativas: sem duplicatas e sem conflito com palavras-chave positivas.

        Responda exclusivamente em JSON valido neste formato:
        {{formato}}
        """;
    }
}
