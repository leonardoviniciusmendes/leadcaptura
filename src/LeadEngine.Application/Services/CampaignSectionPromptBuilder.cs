using System.Text.Json;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Services;

public sealed class CampaignSectionPromptBuilder
{
    public string Build(Campanha campanha, CampanhaSecao secao, string? instrucaoAdicional)
    {
        var atual = CampanhaContentSnapshot.From(campanha);
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
        Voce e um especialista em campanhas de Google Ads para planos de saude no Brasil.
        Regenere somente a secao solicitada da campanha. Nao gere a campanha inteira.

        Briefing original:
        - Tipo de publico: {{campanha.TipoPublico}}
        - Cidade: {{campanha.Cidade}}
        - Estado: {{campanha.Estado}}
        - Bairro ou regiao: {{campanha.Regiao ?? "nao informado"}}
        - Operadora: {{campanha.Operadora}}
        - Orcamento diario: {{campanha.OrcamentoDiario}}
        - Objetivo ou observacao: {{campanha.Objetivo ?? "nao informado"}}

        Conteudo atual da campanha:
        {{JsonSerializer.Serialize(atual)}}

        Secao a regenerar: {{secao}}
        Instrucao adicional: {{instrucaoAdicional ?? "nao informada"}}

        Regras comerciais e restricoes de saude:
        - Use portugues do Brasil.
        - Seja claro, profissional e objetivo.
        - Nao faca promessa enganosa.
        - Nao garanta preco, economia, cobertura, aprovacao, contratacao ou ausencia de carencia.
        - Nao invente informacoes sobre operadoras, rede credenciada, hospitais ou coberturas.
        - Mensagem de WhatsApp nao deve prometer preco, aprovacao, cobertura ou carencia.
        - Beneficios nao devem conter promessa garantida.
        - FAQ deve esclarecer variacao de preco, rede, cobertura, carencia e disponibilidade quando pertinente.
        - Palavras-chave devem ter intencao comercial e evitar termos excessivamente amplos.
        - Palavras negativas devem evitar intencao baixa como emprego, salario, concurso, segunda via, login e boleto.
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
