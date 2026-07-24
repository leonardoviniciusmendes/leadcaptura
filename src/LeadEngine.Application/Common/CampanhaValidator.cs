using LeadEngine.Application.DTOs;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Common;

public static class CampanhaValidator
{
    private static readonly HashSet<string> OperadorasPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        "Nenhuma específica",
        "Amil",
        "Bradesco Saúde",
        "SulAmérica",
        "Unimed",
        "Outra"
    };

    public static void ValidarBriefing(GerarCampanhaRequest request)
    {
        var erros = new List<string>();

        if (!Enum.IsDefined(request.TipoPublico) || request.TipoPublico == 0)
        {
            erros.Add("Tipo de público obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(request.Cidade))
        {
            erros.Add("Cidade obrigatória.");
        }

        if (string.IsNullOrWhiteSpace(request.Estado))
        {
            erros.Add("Estado obrigatório.");
        }

        if (request.OrcamentoDiario <= 0)
        {
            erros.Add("Orçamento diário deve ser maior que zero.");
        }

        if (string.IsNullOrWhiteSpace(request.Operadora))
        {
            erros.Add("Operadora obrigatória.");
        }
        else if (!OperadorasPermitidas.Contains(request.Operadora))
        {
            erros.Add("Operadora inválida.");
        }

        if (string.Equals(request.Operadora, "Outra", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(request.OperadoraOutra))
        {
            erros.Add("Informe o nome da operadora.");
        }

        ValidarTamanho(request.Cidade, 120, "Cidade", erros);
        ValidarTamanho(request.Estado, 2, "Estado", erros);
        ValidarTamanho(request.Regiao, 120, "Bairro ou região", erros);
        ValidarTamanho(request.Operadora, 80, "Operadora", erros);
        ValidarTamanho(request.OperadoraOutra, 80, "Nome da operadora", erros);
        ValidarTamanho(request.Objetivo, 500, "Objetivo ou observação", erros);

        if (erros.Count > 0)
        {
            throw new ArgumentException(string.Join(" ", erros));
        }
    }

    public static void ValidarRevisao(RevisarCampanhaRequest request)
    {
        var erros = new List<string>();

        ValidarObrigatorio(request.Nome, "Nome", erros);
        ValidarObrigatorio(request.TituloLandingPage, "Título da landing page", erros);
        ValidarObrigatorio(request.SubtituloLandingPage, "Subtítulo da landing page", erros);
        ValidarObrigatorio(request.TextoBotao, "Texto do botão", erros);
        ValidarObrigatorio(request.MensagemWhatsApp, "Mensagem de WhatsApp", erros);
        ValidarObrigatorio(request.Slug, "Slug", erros);

        if (!Enum.IsDefined(request.Status))
        {
            erros.Add("Status inválido.");
        }

        ValidarTamanho(request.Nome, 180, "Nome", erros);
        ValidarTamanho(request.TituloLandingPage, 180, "Título da landing page", erros);
        ValidarTamanho(request.SubtituloLandingPage, 300, "Subtítulo da landing page", erros);
        ValidarTamanho(request.TextoBotao, 80, "Texto do botão", erros);
        ValidarTamanho(request.MensagemWhatsApp, 500, "Mensagem de WhatsApp", erros);
        ValidarTamanho(request.Slug, 180, "Slug", erros);
        ValidarTamanho(request.Objetivo, 500, "Objetivo ou observação", erros);

        if (erros.Count > 0)
        {
            throw new ArgumentException(string.Join(" ", erros));
        }
    }

    public static string OperadoraEfetiva(GerarCampanhaRequest request)
    {
        return string.Equals(request.Operadora, "Outra", StringComparison.OrdinalIgnoreCase)
            ? request.OperadoraOutra!.Trim()
            : request.Operadora.Trim();
    }

    private static void ValidarObrigatorio(string? value, string campo, ICollection<string> erros)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            erros.Add($"{campo} obrigatório.");
        }
    }

    private static void ValidarTamanho(string? value, int max, string campo, ICollection<string> erros)
    {
        if (value?.Length > max)
        {
            erros.Add($"{campo} deve ter no máximo {max} caracteres.");
        }
    }
}
