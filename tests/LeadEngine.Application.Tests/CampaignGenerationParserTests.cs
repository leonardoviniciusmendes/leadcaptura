using LeadEngine.Application.Common;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Tests;

public sealed class CampaignGenerationParserTests
{
    [Fact]
    public void PromptBuilder_IncluiRegrasDeSeguranca()
    {
        var prompt = new CampaignPromptBuilder().Build(CampanhaServiceTests.BriefingPadrao());

        Assert.Contains("Não garanta preço", prompt);
        Assert.Contains("português do Brasil", prompt);
        Assert.Contains("Rio de Janeiro", prompt);
    }

    [Fact]
    public void Parser_DesserializaRespostaValida()
    {
        var result = Parser().Parse(JsonValido(), "OpenRouter", "modelo", 120);

        Assert.Equal("Plano Familiar Amil - Barra", result.Nome);
        Assert.Equal("plano-familiar-amil-barra", result.Slug);
        Assert.Equal(8, result.TitulosAnuncios.Count);
        Assert.Equal(3, result.DescricoesAnuncios.Count);
    }

    [Fact]
    public void Parser_RejeitaJsonInvalido()
    {
        Assert.Throws<CampaignGenerationException>(() => Parser().Parse("{", "OpenRouter", "modelo", 1));
    }

    [Fact]
    public void Parser_RejeitaRespostaIncompleta()
    {
        Assert.Throws<CampaignGenerationException>(() => Parser().Parse("""{"nome":"Teste"}""", "OpenRouter", "modelo", 1));
    }

    [Fact]
    public void Parser_NormalizaSlug()
    {
        var result = Parser().Parse(JsonValido().Replace("plano-familiar-amil-barra", "Plano Saúde Família Barra!"), "OpenRouter", "modelo", 1);

        Assert.Equal("plano-saude-familia-barra", result.Slug);
    }

    [Fact]
    public void Parser_LimitaTitulos()
    {
        var result = Parser().Parse(JsonValidoComTitulosLongos(), "OpenRouter", "modelo", 1);

        Assert.All(result.TitulosAnuncios, title => Assert.True(title.Length <= 30));
    }

    [Fact]
    public void Parser_LimitaDescricoes()
    {
        var result = Parser().Parse(JsonValidoComDescricoesLongas(), "OpenRouter", "modelo", 1);

        Assert.All(result.DescricoesAnuncios, description => Assert.True(description.Length <= 90));
    }

    public static string JsonValido() => """
    {
      "nome": "Plano Familiar Amil - Barra",
      "slug": "plano-familiar-amil-barra",
      "tituloLandingPage": "Plano de Saúde Familiar na Barra",
      "subtituloLandingPage": "Compare opções com atendimento personalizado.",
      "textoBotao": "Solicitar cotação",
      "mensagemWhatsApp": "Olá, gostaria de uma cotação.",
      "beneficios": ["Atendimento consultivo", "Cotação por perfil", "Comparação regional"],
      "perguntasFrequentes": [
        { "pergunta": "O preço é fixo?", "resposta": "Não. Preços variam por idade, região e contratação." },
        { "pergunta": "A rede é garantida?", "resposta": "Não. Rede e cobertura dependem do plano." },
        { "pergunta": "Existe carência?", "resposta": "Carência depende das condições da operadora." }
      ],
      "palavrasChave": ["plano de saúde familiar", "cotação plano saúde", "plano amil barra"],
      "palavrasChaveNegativas": ["emprego", "salário", "concurso", "boleto"],
      "titulosAnuncios": ["Plano Saúde", "Cotação Amil", "Plano Familiar", "Fale no WhatsApp", "Atendimento RJ", "Compare Planos", "Cotação Rápida", "Planos na Barra"],
      "descricoesAnuncios": ["Compare opções conforme seu perfil.", "Atendimento consultivo para planos de saúde.", "Solicite cotação pelo WhatsApp."]
    }
    """;

    private static string JsonValidoComTitulosLongos()
    {
        return JsonValido().Replace("Plano Saúde", "Plano de Saúde Familiar Muito Longo");
    }

    private static string JsonValidoComDescricoesLongas()
    {
        return JsonValido().Replace("Compare opções conforme seu perfil.", new string('a', 120));
    }

    private static CampaignGenerationResponseParser Parser() => new();
}
