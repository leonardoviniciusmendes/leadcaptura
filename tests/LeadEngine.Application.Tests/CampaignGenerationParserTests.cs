using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Tests;

public sealed class CampaignGenerationParserTests
{
    [Fact]
    public void PromptBuilder_IncluiRegrasDeSeguranca()
    {
        var prompt = new CampaignPromptBuilder().Build(ContextoSaude());

        Assert.Contains("Nao faca promessas enganosas", prompt);
        Assert.Contains("portugues do Brasil", prompt);
        Assert.Contains("Rio de Janeiro", prompt);
    }

    [Fact]
    public void PromptBuilder_NovoSegmentoNaoRecebeTermosDeSaude()
    {
        var prompt = new CampaignPromptBuilder().Build(ContextoGenerico());

        Assert.DoesNotContain("plano de saude", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("carencia", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hospital", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Oficina mecanica", prompt);
    }

    [Fact]
    public void PromptBuilder_IncluiDefaultConfigCampaignConfigERestricoes()
    {
        var prompt = new CampaignPromptBuilder().Build(ContextoGenerico());

        Assert.Contains("\"defaultGoal\":\"appointment_booking\"", prompt);
        Assert.Contains("\"productOrService\":\"Revisao automotiva\"", prompt);
        Assert.Contains("nao prometer diagnostico sem avaliacao", prompt);
    }

    [Fact]
    public void PromptBuilder_NaoUsaSlugComoRegraDeNegocio()
    {
        var prompt = new CampaignPromptBuilder().Build(ContextoGenerico() with
        {
            SegmentSlug = "slug-com-termo-isca",
            SegmentName = "Assistencia local"
        });

        Assert.Contains("Slug do segmento: slug-com-termo-isca", prompt);
        Assert.Contains("Use o TemplateKey apenas para orientar o formato comercial da oferta, nunca como segmento.", prompt);
    }

    [Fact]
    public void PromptBuilder_TemplateKeyPodeSerReutilizadoPorSegmentosDiferentes()
    {
        var oficina = ContextoGenerico() with { SegmentName = "Oficina mecanica", SegmentSlug = "oficina", TemplateKey = "appointment_booking" };
        var fisioterapia = ContextoGenerico() with { SegmentName = "Fisioterapia", SegmentSlug = "fisioterapia", TemplateKey = "appointment_booking" };

        var promptOficina = new CampaignPromptBuilder().Build(oficina);
        var promptFisioterapia = new CampaignPromptBuilder().Build(fisioterapia);

        Assert.Contains("TemplateKey: appointment_booking", promptOficina);
        Assert.Contains("TemplateKey: appointment_booking", promptFisioterapia);
        Assert.Contains("Oficina mecanica", promptOficina);
        Assert.Contains("Fisioterapia", promptFisioterapia);
    }

    [Fact]
    public void ContextFactory_JsonInvalidoGeraErroControlado()
    {
        var segment = new Segment
        {
            Name = "Segmento",
            Slug = "segmento",
            TemplateKey = "local_service_lead_generation",
            DefaultConfigJson = "{"
        };

        var ex = Assert.Throws<CampaignGenerationException>(() =>
            CampaignGenerationContextFactory.FromRequest(CampanhaServiceTests.BriefingPadrao(), segment, null));

        Assert.Contains("DefaultConfigJson do segmento 'segmento' nao contem JSON valido.", ex.Message);
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
        var result = Parser().Parse(JsonValido().Replace("plano-familiar-amil-barra", "Plano Saude Familia Barra!"), "OpenRouter", "modelo", 1);

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
      "tituloLandingPage": "Plano de Saude Familiar na Barra",
      "subtituloLandingPage": "Compare opcoes com atendimento personalizado.",
      "textoBotao": "Solicitar cotacao",
      "mensagemWhatsApp": "Ola, gostaria de uma cotacao.",
      "beneficios": ["Atendimento consultivo", "Cotacao por perfil", "Comparacao regional"],
      "perguntasFrequentes": [
        { "pergunta": "O preco e fixo?", "resposta": "Nao. Precos variam por idade, regiao e contratacao." },
        { "pergunta": "A rede e garantida?", "resposta": "Nao. Rede e cobertura dependem do plano." },
        { "pergunta": "Existe carencia?", "resposta": "Carencia depende das condicoes da operadora." }
      ],
      "palavrasChave": ["plano de saude familiar", "cotacao plano saude", "plano amil barra"],
      "palavrasChaveNegativas": ["emprego", "salario", "concurso", "boleto"],
      "titulosAnuncios": ["Plano Saude", "Cotacao Amil", "Plano Familiar", "Fale no WhatsApp", "Atendimento RJ", "Compare Planos", "Cotacao Rapida", "Planos na Barra"],
      "descricoesAnuncios": ["Compare opcoes conforme seu perfil.", "Atendimento consultivo para planos de saude.", "Solicite cotacao pelo WhatsApp."]
    }
    """;

    private static string JsonValidoComTitulosLongos()
    {
        return JsonValido().Replace("Plano Saude", "Plano de Saude Familiar Muito Longo");
    }

    private static string JsonValidoComDescricoesLongas()
    {
        return JsonValido().Replace("Compare opcoes conforme seu perfil.", new string('a', 120));
    }

    private static CampaignGenerationContext ContextoSaude()
    {
        return CampaignGenerationContextFactory.FromRequest(
            CampanhaServiceTests.BriefingPadrao(),
            new Segment
            {
                Name = "Planos de Saude",
                Slug = "planos-saude",
                TemplateKey = "high_ticket_quote",
                DefaultConfigJson = """{"restrictions":["nao garantir preco"]}"""
            },
            null);
    }

    private static CampaignGenerationContext ContextoGenerico()
    {
        var request = CampanhaServiceTests.BriefingPadrao() with
        {
            BusinessDescription = "Oficina mecanica especializada em revisoes",
            ProductOrService = "Revisao automotiva",
            TargetAudience = "Motoristas da regiao",
            CampaignGoal = "Agendar avaliacao",
            Offer = "Checklist inicial",
            BrandTone = "Objetivo",
            Restrictions = ["nao prometer diagnostico sem avaliacao"],
            Location = new CampaignLocationDto("Campinas", "SP", "Cambuí")
        };

        return CampaignGenerationContextFactory.FromRequest(
            request,
            new Segment
            {
                Name = "Oficina mecanica",
                Slug = "oficina-mecanica",
                TemplateKey = "appointment_booking",
                DefaultConfigJson = """{"defaultGoal":"appointment_booking"}"""
            },
            """{"productOrService":"Revisao automotiva","campaignGoal":"Agendar avaliacao"}""");
    }

    private static CampaignGenerationResponseParser Parser() => new();
}
