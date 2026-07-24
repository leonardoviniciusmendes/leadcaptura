using LeadEngine.Application.DTOs;
using LeadEngine.Domain.Enums;
using System.Globalization;
using System.Text;

namespace LeadEngine.Application.Common;

public static class CampanhaValidator
{
    private static readonly HashSet<string> OperadorasPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        "Nenhuma especifica",
        "Nenhuma especifica",
        "Amil",
        "Bradesco Saude",
        "SulAmerica",
        "Unimed",
        "Outra"
    };

    public static void ValidarBriefing(GerarCampanhaRequest request)
    {
        var erros = new List<string>();

        if (!Enum.IsDefined(request.TipoPublico) || request.TipoPublico == 0)
        {
            erros.Add("Tipo de publico obrigatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.Cidade))
        {
            erros.Add("Cidade obrigatoria.");
        }

        if (string.IsNullOrWhiteSpace(request.Estado))
        {
            erros.Add("Estado obrigatorio.");
        }

        if (request.OrcamentoDiario <= 0)
        {
            erros.Add("Orcamento diario deve ser maior que zero.");
        }

        if (string.IsNullOrWhiteSpace(request.Operadora))
        {
            erros.Add("Operadora obrigatoria.");
        }
        else if (!OperadorasPermitidas.Contains(RemoveAccents(request.Operadora)))
        {
            erros.Add("Operadora invalida.");
        }

        if (string.Equals(request.Operadora, "Outra", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(request.OperadoraOutra))
        {
            erros.Add("Informe o nome da operadora.");
        }

        ValidarTamanho(request.Cidade, 120, "Cidade", erros);
        ValidarTamanho(request.Estado, 2, "Estado", erros);
        ValidarTamanho(request.Regiao, 120, "Bairro ou regiao", erros);
        ValidarTamanho(request.Operadora, 80, "Operadora", erros);
        ValidarTamanho(request.OperadoraOutra, 80, "Nome da operadora", erros);
        ValidarTamanho(request.Objetivo, 500, "Objetivo ou observacao", erros);

        if (erros.Count > 0)
        {
            throw new ArgumentException(string.Join(" ", erros));
        }
    }

    public static void ValidarRevisao(RevisarCampanhaRequest request)
    {
        var conteudo = NormalizarEValidarConteudo(
            request.TituloLandingPage,
            request.SubtituloLandingPage,
            request.TextoBotao,
            request.MensagemWhatsApp,
            request.Beneficios,
            request.PerguntasFrequentes.Select(x => new FaqItemValidation(x.Pergunta, x.Resposta)),
            request.PalavrasChave,
            request.PalavrasChaveNegativas,
            request.TitulosAnuncios,
            request.DescricoesAnuncios);

        var erros = new List<string>();
        ValidarObrigatorio(request.Nome, "Nome", erros);
        ValidarTamanho(request.Nome, 180, "Nome", erros);
        ValidarCampanhaCompleta(conteudo, erros);

        if (erros.Count > 0)
        {
            throw new ArgumentException(string.Join(" ", erros));
        }
    }

    public static CampanhaConteudoNormalizado NormalizarEValidarConteudo(
        string tituloLandingPage,
        string subtituloLandingPage,
        string textoBotao,
        string mensagemWhatsApp,
        IEnumerable<string>? beneficios,
        IEnumerable<FaqItemValidation>? perguntasFrequentes,
        IEnumerable<string>? palavrasChave,
        IEnumerable<string>? palavrasChaveNegativas,
        IEnumerable<string>? titulosAnuncios,
        IEnumerable<string>? descricoesAnuncios)
    {
        var conteudo = new CampanhaConteudoNormalizado(
            CampanhaText.Limitar(tituloLandingPage, 180) ?? string.Empty,
            CampanhaText.Limitar(subtituloLandingPage, 300) ?? string.Empty,
            CampanhaText.Limitar(textoBotao, 80) ?? string.Empty,
            CampanhaText.Limitar(mensagemWhatsApp, 500) ?? string.Empty,
            NormalizarLista(beneficios, 120),
            NormalizarFaq(perguntasFrequentes),
            NormalizarLista(palavrasChave, 120),
            NormalizarLista(palavrasChaveNegativas, 120),
            NormalizarLista(titulosAnuncios, 30),
            NormalizarLista(descricoesAnuncios, 90));

        ValidarCampanhaCompleta(conteudo);
        return conteudo;
    }

    public static void ValidarCampanhaCompleta(CampanhaConteudoNormalizado conteudo)
    {
        var erros = new List<string>();
        ValidarCampanhaCompleta(conteudo, erros);

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

    private static void ValidarCampanhaCompleta(CampanhaConteudoNormalizado conteudo, ICollection<string> erros)
    {
        ValidarObrigatorio(conteudo.TituloLandingPage, "Titulo da landing page", erros);
        ValidarObrigatorio(conteudo.SubtituloLandingPage, "Subtitulo da landing page", erros);
        ValidarObrigatorio(conteudo.TextoBotao, "Texto do botao", erros);
        ValidarObrigatorio(conteudo.MensagemWhatsApp, "Mensagem de WhatsApp", erros);

        if (ContemPromessaProibida(conteudo.MensagemWhatsApp))
        {
            erros.Add("Mensagem de WhatsApp nao deve prometer preco, aprovacao, cobertura ou carencia.");
        }

        ValidarQuantidade("Titulos", conteudo.TitulosAnuncios, 8, 12, erros);
        ValidarTamanhoItens("Titulos", conteudo.TitulosAnuncios, 30, erros);
        ValidarDuplicatas("Titulos", conteudo.TitulosAnuncios, erros);

        ValidarQuantidade("Descricoes", conteudo.DescricoesAnuncios, 3, 4, erros);
        ValidarTamanhoItens("Descricoes", conteudo.DescricoesAnuncios, 90, erros);
        ValidarDuplicatas("Descricoes", conteudo.DescricoesAnuncios, erros);

        ValidarQuantidade("Beneficios", conteudo.Beneficios, 3, 6, erros);
        if (conteudo.Beneficios.Any(ContemPromessaProibida))
        {
            erros.Add("Beneficios nao devem conter promessas garantidas.");
        }

        ValidarQuantidade("FAQ", conteudo.PerguntasFrequentes, 3, 6, erros);
        if (conteudo.PerguntasFrequentes.Any(x => string.IsNullOrWhiteSpace(x.Pergunta) || string.IsNullOrWhiteSpace(x.Resposta)))
        {
            erros.Add("FAQ deve ter pergunta e resposta obrigatorias.");
        }

        if (conteudo.PalavrasChave.Count < 3)
        {
            erros.Add("Palavras-chave devem conter pelo menos 3 itens.");
        }

        ValidarDuplicatas("Palavras-chave", conteudo.PalavrasChave, erros);
        ValidarDuplicatas("Palavras negativas", conteudo.PalavrasChaveNegativas, erros);

        var positivas = new HashSet<string>(conteudo.PalavrasChave, StringComparer.OrdinalIgnoreCase);
        if (conteudo.PalavrasChaveNegativas.Any(positivas.Contains))
        {
            erros.Add("Uma palavra nao pode estar simultaneamente como positiva e negativa.");
        }
    }

    private static IReadOnlyList<string> NormalizarLista(IEnumerable<string>? values, int maxLength)
    {
        return (values ?? [])
            .Select(x => CampanhaText.Limitar(x, maxLength))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToArray();
    }

    private static IReadOnlyList<FaqItemValidation> NormalizarFaq(IEnumerable<FaqItemValidation>? values)
    {
        return (values ?? [])
            .Select(x => new FaqItemValidation(CampanhaText.Limitar(x.Pergunta, 180) ?? string.Empty, CampanhaText.Limitar(x.Resposta, 500) ?? string.Empty))
            .Where(x => !string.IsNullOrWhiteSpace(x.Pergunta) || !string.IsNullOrWhiteSpace(x.Resposta))
            .ToArray();
    }

    private static void ValidarObrigatorio(string? value, string campo, ICollection<string> erros)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            erros.Add($"{campo} obrigatorio.");
        }
    }

    private static void ValidarTamanho(string? value, int max, string campo, ICollection<string> erros)
    {
        if (value?.Length > max)
        {
            erros.Add($"{campo} deve ter no maximo {max} caracteres.");
        }
    }

    private static void ValidarQuantidade<T>(string campo, IReadOnlyList<T> values, int min, int max, ICollection<string> erros)
    {
        if (values.Count < min || values.Count > max)
        {
            erros.Add($"{campo} deve conter entre {min} e {max} itens.");
        }
    }

    private static void ValidarTamanhoItens(string campo, IReadOnlyList<string> values, int max, ICollection<string> erros)
    {
        if (values.Any(x => x.Length > max))
        {
            erros.Add($"{campo} deve ter itens com no maximo {max} caracteres.");
        }
    }

    private static void ValidarDuplicatas(string campo, IReadOnlyList<string> values, ICollection<string> erros)
    {
        if (values.Count != values.Distinct(StringComparer.OrdinalIgnoreCase).Count())
        {
            erros.Add($"{campo} nao deve conter duplicatas.");
        }
    }

    private static bool ContemPromessaProibida(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = RemoveAccents(value).ToLowerInvariant();
        return normalized.Contains("garant", StringComparison.Ordinal)
            || normalized.Contains("menor preco", StringComparison.Ordinal)
            || normalized.Contains("preco fixo", StringComparison.Ordinal)
            || normalized.Contains("aprovacao", StringComparison.Ordinal)
            || normalized.Contains("cobertura", StringComparison.Ordinal)
            || normalized.Contains("carencia zero", StringComparison.Ordinal)
            || normalized.Contains("sem carencia", StringComparison.Ordinal);
    }

    private static string RemoveAccents(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}

public sealed record FaqItemValidation(string Pergunta, string Resposta);

public sealed record CampanhaConteudoNormalizado(
    string TituloLandingPage,
    string SubtituloLandingPage,
    string TextoBotao,
    string MensagemWhatsApp,
    IReadOnlyList<string> Beneficios,
    IReadOnlyList<FaqItemValidation> PerguntasFrequentes,
    IReadOnlyList<string> PalavrasChave,
    IReadOnlyList<string> PalavrasChaveNegativas,
    IReadOnlyList<string> TitulosAnuncios,
    IReadOnlyList<string> DescricoesAnuncios);
