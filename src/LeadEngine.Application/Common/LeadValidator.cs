using System.ComponentModel.DataAnnotations;
using LeadEngine.Application.DTOs;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Common;

public static class LeadValidator
{
    public static IReadOnlyCollection<string> Validar(CriarLeadRequest request)
    {
        var erros = new List<string>();
        if (!Enum.IsDefined(request.Tipo) || request.Tipo == 0)
        {
            erros.Add("Tipo de lead obrigatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            erros.Add("Nome obrigatorio.");
        }

        var whatsapp = LeadSanitizer.Digitos(request.WhatsApp);
        if (whatsapp.Length is < 10 or > 13)
        {
            erros.Add("WhatsApp deve conter DDD.");
        }

        var cep = LeadSanitizer.Digitos(request.Cep);
        if (!string.IsNullOrEmpty(cep) && cep.Length != 8)
        {
            erros.Add("CEP deve conter oito digitos.");
        }

        if (request.QuantidadeVidas is <= 0)
        {
            erros.Add("Quantidade de vidas deve ser maior que zero.");
        }

        if (request.QuantidadeFuncionarios is <= 0)
        {
            erros.Add("Quantidade de funcionarios deve ser maior que zero.");
        }

        if (request.Idades is { Count: > 20 })
        {
            erros.Add("Informe no maximo 20 idades.");
        }

        if (request.Idades?.Any(i => i is < 0 or > 120) == true)
        {
            erros.Add("Idades devem estar entre 0 e 120.");
        }

        if (!string.IsNullOrWhiteSpace(request.Email) && !new EmailAddressAttribute().IsValid(request.Email))
        {
            erros.Add("E-mail invalido.");
        }

        var cnpj = LeadSanitizer.Digitos(request.Cnpj);
        if (!string.IsNullOrEmpty(cnpj) && !CnpjValido(cnpj))
        {
            erros.Add("CNPJ invalido.");
        }

        if (!request.ConsentimentoContato)
        {
            erros.Add("Consentimento de contato obrigatorio.");
        }

        if (!string.IsNullOrWhiteSpace(request.Honeypot))
        {
            erros.Add("Formulario invalido.");
        }

        ValidarTamanho(request.Nome, 150, "Nome", erros);
        ValidarTamanho(request.Email, 180, "E-mail", erros);
        ValidarTamanho(request.Cidade, 120, "Cidade", erros);
        ValidarTamanho(request.Uf, 2, "UF", erros);
        ValidarTamanho(request.HospitalDesejado, 120, "Hospital desejado", erros);
        ValidarTamanho(request.OperadoraDesejada, 120, "Operadora desejada", erros);
        ValidarTamanho(request.PlanoAtual, 120, "Plano atual", erros);
        ValidarTamanho(request.NomeEmpresa, 180, "Nome da empresa", erros);

        if (request.Tipo is TipoLead.Empresa or TipoLead.Mei && string.IsNullOrWhiteSpace(request.NomeEmpresa))
        {
            erros.Add("Nome da empresa obrigatorio para lead empresarial ou MEI.");
        }

        return erros;
    }

    private static void ValidarTamanho(string? value, int max, string campo, ICollection<string> erros)
    {
        if (value?.Length > max)
        {
            erros.Add($"{campo} deve ter no maximo {max} caracteres.");
        }
    }

    private static bool CnpjValido(string cnpj)
    {
        if (cnpj.Length != 14 || cnpj.Distinct().Count() == 1)
        {
            return false;
        }

        int[] mult1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] mult2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        var temp = cnpj[..12];
        var soma = temp.Select((t, i) => (t - '0') * mult1[i]).Sum();
        var resto = soma % 11;
        var digito = resto < 2 ? 0 : 11 - resto;
        temp += digito;
        soma = temp.Select((t, i) => (t - '0') * mult2[i]).Sum();
        resto = soma % 11;
        digito = resto < 2 ? 0 : 11 - resto;
        return cnpj.EndsWith(digito.ToString(), StringComparison.Ordinal);
    }
}
