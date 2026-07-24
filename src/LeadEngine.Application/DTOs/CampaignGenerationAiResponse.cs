using System.Text.Json.Serialization;

namespace LeadEngine.Application.DTOs;

public sealed class CampaignGenerationAiResponse
{
    [JsonPropertyName("nome")]
    public string? Nome { get; set; }

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("tituloLandingPage")]
    public string? TituloLandingPage { get; set; }

    [JsonPropertyName("subtituloLandingPage")]
    public string? SubtituloLandingPage { get; set; }

    [JsonPropertyName("textoBotao")]
    public string? TextoBotao { get; set; }

    [JsonPropertyName("mensagemWhatsApp")]
    public string? MensagemWhatsApp { get; set; }

    [JsonPropertyName("beneficios")]
    public List<string>? Beneficios { get; set; }

    [JsonPropertyName("perguntasFrequentes")]
    public List<CampaignGenerationAiFaq>? PerguntasFrequentes { get; set; }

    [JsonPropertyName("palavrasChave")]
    public List<string>? PalavrasChave { get; set; }

    [JsonPropertyName("palavrasChaveNegativas")]
    public List<string>? PalavrasChaveNegativas { get; set; }

    [JsonPropertyName("titulosAnuncios")]
    public List<string>? TitulosAnuncios { get; set; }

    [JsonPropertyName("descricoesAnuncios")]
    public List<string>? DescricoesAnuncios { get; set; }
}

public sealed class CampaignGenerationAiFaq
{
    [JsonPropertyName("pergunta")]
    public string? Pergunta { get; set; }

    [JsonPropertyName("resposta")]
    public string? Resposta { get; set; }
}
