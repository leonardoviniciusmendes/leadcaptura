using System.Net.Mail;
using System.Text.Json;
using System.Text.RegularExpressions;
using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;
using Microsoft.Extensions.Options;

namespace LeadEngine.Application.Services;

public sealed class LeadService(
    ICampanhaRepository campanhaRepository,
    ILeadRepository leadRepository,
    IWhatsAppUrlBuilder whatsAppUrlBuilder,
    IRequestContext requestContext,
    IOptions<LeadCaptureOptions> options,
    IConfigurationResolver? resolver = null) : ILeadService
{
    public async Task<CampanhaPublicaResponse?> ObterCampanhaPublicaAsync(string slug, CancellationToken cancellationToken)
    {
        var campanha = await campanhaRepository.ObterPublicadaPorSlugAsync(NormalizeSlug(slug), cancellationToken);
        return campanha is null ? null : ToPublicResponse(campanha);
    }

    public async Task<CapturarLeadPublicoResponse> CapturarLeadPublicoAsync(string slug, CapturarLeadPublicoRequest request, CancellationToken cancellationToken)
    {
        var campanha = await campanhaRepository.ObterPublicadaPorSlugAsync(NormalizeSlug(slug), cancellationToken)
            ?? throw new KeyNotFoundException("Campanha nao encontrada.");

        if (!string.IsNullOrWhiteSpace(request.Website))
        {
            return new CapturarLeadPublicoResponse(Guid.Empty, "Lead registrado com sucesso.", whatsAppUrlBuilder.Build(FakeLead(request), campanha), false);
        }

        ValidateAntiSpam(request);

        var form = LeadFormSchema.GetEffectiveForm(campanha);
        var normalized = NormalizeAndValidateForm(campanha, form, request);
        var telefone = LeadSanitizer.Digitos(normalized.Phone);
        var leadOptions = await EffectiveOptionsAsync(cancellationToken);
        var janela = DateTime.UtcNow.AddHours(-Math.Max(1, leadOptions.DuplicateWindowHours));
        var duplicado = await leadRepository.ObterDuplicadoRecenteAsync(campanha.Id, telefone, janela, cancellationToken);
        if (duplicado is not null)
        {
            return new CapturarLeadPublicoResponse(duplicado.Id, "Lead registrado com sucesso.", whatsAppUrlBuilder.Build(duplicado, campanha), false);
        }

        var lead = CriarLead(campanha, request, normalized, telefone);
        await leadRepository.AdicionarAsync(lead, cancellationToken);
        await leadRepository.SalvarAsync(cancellationToken);

        return new CapturarLeadPublicoResponse(lead.Id, "Lead registrado com sucesso.", whatsAppUrlBuilder.Build(lead, campanha), true);
    }

    private Lead CriarLead(Campanha campanha, CapturarLeadPublicoRequest request, NormalizedLeadForm normalized, string telefone)
    {
        var now = DateTime.UtcNow;
        var email = LeadSanitizer.Email(normalized.Email);
        var estado = LeadSanitizer.Texto(normalized.State, 2)?.ToUpperInvariant();
        var origemLanding = $"/lp/{campanha.Slug}";
        var usesLegacy = SegmentMapping.UsesLegacyBriefing(campanha.Segment);

        var lead = new Lead
        {
            Id = Guid.NewGuid(),
            CampanhaId = campanha.Id,
            Tipo = usesLegacy ? ToTipoLead(request.TipoContratacao) : TipoLead.PessoaFisica,
            TipoContratacao = usesLegacy ? request.TipoContratacao : null,
            Nome = LeadSanitizer.Texto(normalized.Name, 120)!,
            WhatsApp = telefone,
            WhatsAppNormalizado = telefone,
            Email = email,
            EmailNormalizado = email,
            Cidade = LeadSanitizer.Texto(normalized.City, 100),
            Uf = estado,
            QuantidadeVidas = usesLegacy ? request.QuantidadeVidas : ReadIntAnswer(normalized.Answers, "quantidadeVidas"),
            Observacao = LeadSanitizer.Texto(request.Observacao, 1000),
            Status = StatusLead.Recebido,
            ConsentimentoContato = request.Consentimento,
            ConsentimentoEm = now,
            TextoConsentimentoVersao = EffectiveOptionsSync().ConsentVersion,
            CriadoEm = now,
            OrigemCaptura = "LandingPage",
            IpHash = requestContext.IpHash,
            UserAgentResumo = LeadSanitizer.Texto(requestContext.UserAgent, 300),
            UtmSource = LeadSanitizer.Texto(request.UtmSource, 100),
            UtmMedium = LeadSanitizer.Texto(request.UtmMedium, 100),
            UtmCampaign = LeadSanitizer.Texto(request.UtmCampaign, 180),
            UtmTerm = LeadSanitizer.Texto(request.UtmTerm, 180),
            UtmContent = LeadSanitizer.Texto(request.UtmContent, 180),
            Gclid = LeadSanitizer.Texto(request.Gclid, 180),
            Fbclid = LeadSanitizer.Texto(request.Fbclid, 180),
            StatusEnvioExterno = "Pendente",
            TentativasEnvioExterno = 0,
            Origem = new OrigemLead
            {
                Id = Guid.NewGuid(),
                UtmSource = LeadSanitizer.Texto(request.UtmSource, 100),
                UtmMedium = LeadSanitizer.Texto(request.UtmMedium, 100),
                UtmCampaign = LeadSanitizer.Texto(request.UtmCampaign, 180),
                UtmTerm = LeadSanitizer.Texto(request.UtmTerm, 180),
                UtmContent = LeadSanitizer.Texto(request.UtmContent, 180),
                Gclid = LeadSanitizer.Texto(request.Gclid, 180),
                LandingPage = origemLanding,
                UserAgent = LeadSanitizer.Texto(requestContext.UserAgent, 500),
                IpHash = requestContext.IpHash
            }
        };

        foreach (var answer in normalized.Answers)
        {
            lead.Answers.Add(new LeadAnswer
            {
                Id = Guid.NewGuid(),
                LeadId = lead.Id,
                Lead = lead,
                FieldKey = answer.Field.Key,
                LabelSnapshot = answer.Field.Label,
                Type = answer.Field.Type,
                ValueJson = answer.ValueJson
            });
        }

        return lead;
    }

    private static CampanhaPublicaResponse ToPublicResponse(Campanha campanha)
    {
        var snapshot = CampanhaContentSnapshot.From(campanha);
        return new CampanhaPublicaResponse(
            campanha.Nome,
            snapshot.TituloLandingPage,
            snapshot.SubtituloLandingPage,
            snapshot.TextoBotao,
            snapshot.Beneficios,
            snapshot.PerguntasFrequentes.Select(x => new FaqResponse(x.Pergunta, x.Resposta)).ToArray(),
            campanha.Operadora,
            campanha.Cidade,
            campanha.Estado,
            campanha.TipoPublico,
            snapshot.MensagemWhatsApp,
            CampanhaMapping.ToSegmentSummary(campanha),
            CampanhaMapping.ToBriefing(campanha),
            SegmentMapping.UsesLegacyBriefing(campanha.Segment),
            CampanhaMapping.ToLeadForm(campanha));
    }

    private void ValidateAntiSpam(CapturarLeadPublicoRequest request)
    {
        if (request.FormOpenedAt is null)
        {
            throw new ArgumentException("Nao foi possivel registrar a solicitacao.");
        }

        var opened = DateTimeOffset.FromUnixTimeMilliseconds(request.FormOpenedAt.Value);
        if (DateTimeOffset.UtcNow - opened < TimeSpan.FromSeconds(Math.Max(0, EffectiveOptionsSync().MinimumFormSeconds)))
        {
            throw new ArgumentException("Nao foi possivel registrar a solicitacao.");
        }

        if (string.IsNullOrWhiteSpace(requestContext.UserAgent))
        {
            throw new ArgumentException("Nao foi possivel registrar a solicitacao.");
        }
    }

    private static void Validate(CapturarLeadPublicoRequest request)
    {
        var erros = new List<string>();
        var nome = LeadSanitizer.Texto(request.Nome, 120);
        if (string.IsNullOrWhiteSpace(nome) || nome.Length < 2)
        {
            erros.Add("Nome obrigatorio com pelo menos 2 caracteres.");
        }

        var telefone = LeadSanitizer.Digitos(request.Telefone);
        if (telefone.Length is < 10 or > 13)
        {
            erros.Add("Telefone invalido.");
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            if (request.Email.Length > 160)
            {
                erros.Add("Email deve ter no maximo 160 caracteres.");
            }
            else
            {
                try { _ = new MailAddress(request.Email); }
                catch { erros.Add("Email invalido."); }
            }
        }

        if (string.IsNullOrWhiteSpace(request.Cidade))
        {
            erros.Add("Cidade obrigatoria.");
        }

        if (request.Cidade.Length > 100)
        {
            erros.Add("Cidade deve ter no maximo 100 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(request.Estado) || request.Estado.Trim().Length != 2)
        {
            erros.Add("Estado deve ter exatamente 2 caracteres.");
        }

        if (request.QuantidadeVidas is <= 0 or > 999)
        {
            erros.Add("Quantidade de vidas invalida.");
        }

        if (!Enum.IsDefined(request.TipoContratacao))
        {
            erros.Add("Tipo de contratacao invalido.");
        }

        if (request.Observacao?.Length > 1000)
        {
            erros.Add("Observacao deve ter no maximo 1000 caracteres.");
        }

        ValidateMax(request.UtmSource, 100, "utmSource", erros);
        ValidateMax(request.UtmMedium, 100, "utmMedium", erros);
        ValidateMax(request.UtmCampaign, 180, "utmCampaign", erros);
        ValidateMax(request.UtmTerm, 180, "utmTerm", erros);
        ValidateMax(request.UtmContent, 180, "utmContent", erros);
        ValidateMax(request.Gclid, 180, "gclid", erros);
        ValidateMax(request.Fbclid, 180, "fbclid", erros);

        if (erros.Count > 0)
        {
            throw new ArgumentException(string.Join(" ", erros));
        }
    }

    private static NormalizedLeadForm NormalizeAndValidateForm(Campanha campanha, LeadForm form, CapturarLeadPublicoRequest request)
    {
        var answers = request.Answers ?? new Dictionary<string, JsonElement>();
        var errors = new List<string>();
        var normalizedAnswers = new List<NormalizedLeadAnswer>();

        foreach (var field in form.Fields.OrderBy(x => x.Order))
        {
            var value = ReadFieldValue(field, request, answers);
            ValidateField(field, value, errors);
            if (value.HasValue)
            {
                normalizedAnswers.Add(new NormalizedLeadAnswer(field, JsonSerializer.Serialize(value.Value)));
            }
        }

        var name = ReadUniversalString("name", request.Name, request.Nome, answers);
        var phone = ReadUniversalString("phone", request.Phone, request.Telefone, answers);
        var email = ReadUniversalString("email", request.Email, request.Email, answers);
        var city = ReadUniversalString("city", request.Cidade, request.Cidade, answers);
        var state = ReadUniversalString("state", request.Estado, request.Estado, answers);

        if (string.IsNullOrWhiteSpace(LeadSanitizer.Texto(name, 120)) || LeadSanitizer.Texto(name, 120)!.Length < 2)
        {
            errors.Add("Nome obrigatorio com pelo menos 2 caracteres.");
        }

        var telefone = LeadSanitizer.Digitos(phone);
        if (telefone.Length is < 10 or > 13)
        {
            errors.Add("Telefone invalido.");
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            if (email.Length > 160)
            {
                errors.Add("Email deve ter no maximo 160 caracteres.");
            }
            else
            {
                try { _ = new MailAddress(email); }
                catch { errors.Add("Email invalido."); }
            }
        }

        if (SegmentMapping.UsesLegacyBriefing(campanha.Segment))
        {
            Validate(request);
        }

        if (errors.Count > 0)
        {
            throw new ArgumentException(string.Join(" ", errors));
        }

        return new NormalizedLeadForm(name!, phone!, email, city, state, normalizedAnswers);
    }

    private static JsonElement? ReadFieldValue(LeadFormField field, CapturarLeadPublicoRequest request, IReadOnlyDictionary<string, JsonElement> answers)
    {
        if (answers.TryGetValue(field.Key, out var answer))
        {
            return answer;
        }

        return field.Key switch
        {
            "name" => ToElement(request.Name ?? request.Nome),
            "phone" => ToElement(request.Phone ?? request.Telefone),
            "email" => ToElement(request.Email),
            "quantidadeVidas" => ToElement(request.QuantidadeVidas),
            _ => null
        };
    }

    private static string? ReadUniversalString(string key, string? primary, string? legacy, IReadOnlyDictionary<string, JsonElement> answers)
    {
        if (!string.IsNullOrWhiteSpace(primary))
        {
            return primary;
        }

        if (!string.IsNullOrWhiteSpace(legacy))
        {
            return legacy;
        }

        return answers.TryGetValue(key, out var answer) && answer.ValueKind == JsonValueKind.String ? answer.GetString() : null;
    }

    private static void ValidateField(LeadFormField field, JsonElement? value, ICollection<string> errors)
    {
        if (field.Required && IsEmpty(value))
        {
            errors.Add($"{field.Label} obrigatorio.");
            return;
        }

        if (IsEmpty(value))
        {
            return;
        }

        if (!LeadFormSchema.SupportedTypes.Contains(field.Type))
        {
            errors.Add($"{field.Label} possui tipo invalido.");
            return;
        }

        var actualValue = value.GetValueOrDefault();
        switch (field.Type.ToLowerInvariant())
        {
            case "text":
            case "textarea":
            case "date":
                if (actualValue.ValueKind != JsonValueKind.String) errors.Add($"{field.Label} invalido.");
                break;
            case "phone":
                if (actualValue.ValueKind != JsonValueKind.String || LeadSanitizer.Digitos(actualValue.GetString()).Length is < 10 or > 13) errors.Add($"{field.Label} invalido.");
                break;
            case "email":
                if (actualValue.ValueKind != JsonValueKind.String)
                {
                    errors.Add($"{field.Label} invalido.");
                }
                else if (!string.IsNullOrWhiteSpace(actualValue.GetString()))
                {
                    try { _ = new MailAddress(actualValue.GetString()!); }
                    catch { errors.Add($"{field.Label} invalido."); }
                }
                break;
            case "number":
                if (actualValue.ValueKind is not JsonValueKind.Number) errors.Add($"{field.Label} invalido.");
                break;
            case "checkbox":
                if (actualValue.ValueKind is not JsonValueKind.True and not JsonValueKind.False) errors.Add($"{field.Label} invalido.");
                break;
            case "select":
            case "radio":
                ValidateSingleOption(field, actualValue, errors);
                break;
            case "multiselect":
                ValidateMultipleOptions(field, actualValue, errors);
                break;
        }

        ValidateConfiguredRules(field, actualValue, errors);
    }

    private static void ValidateSingleOption(LeadFormField field, JsonElement value, ICollection<string> errors)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            errors.Add($"{field.Label} invalido.");
            return;
        }

        var options = Options(field);
        if (options.Count > 0 && !options.Contains(value.GetString() ?? string.Empty, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"{field.Label} deve conter uma opcao valida.");
        }
    }

    private static void ValidateMultipleOptions(LeadFormField field, JsonElement value, ICollection<string> errors)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            errors.Add($"{field.Label} invalido.");
            return;
        }

        var options = Options(field);
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || (options.Count > 0 && !options.Contains(item.GetString() ?? string.Empty, StringComparer.OrdinalIgnoreCase)))
            {
                errors.Add($"{field.Label} contem opcao invalida.");
                return;
            }
        }
    }

    private static void ValidateConfiguredRules(LeadFormField field, JsonElement value, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(field.ValidationJson))
        {
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(field.ValidationJson);
            var root = doc.RootElement;
            if (value.ValueKind == JsonValueKind.String)
            {
                var text = value.GetString() ?? string.Empty;
                if (root.TryGetProperty("maxLength", out var max) && max.TryGetInt32(out var maxLength) && text.Length > maxLength)
                {
                    errors.Add($"{field.Label} deve ter no maximo {maxLength} caracteres.");
                }
                if (root.TryGetProperty("pattern", out var pattern) && pattern.ValueKind == JsonValueKind.String)
                {
                    var regex = pattern.GetString();
                    if (!string.IsNullOrWhiteSpace(regex) && regex.Length <= 200 && !Regex.IsMatch(text, regex, RegexOptions.None, TimeSpan.FromMilliseconds(100)))
                    {
                        errors.Add($"{field.Label} invalido.");
                    }
                }
            }
            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
            {
                if (root.TryGetProperty("min", out var min) && min.TryGetDecimal(out var minValue) && number < minValue) errors.Add($"{field.Label} invalido.");
                if (root.TryGetProperty("max", out var max) && max.TryGetDecimal(out var maxValue) && number > maxValue) errors.Add($"{field.Label} invalido.");
            }
        }
        catch (JsonException)
        {
            errors.Add($"{field.Label} possui validacao invalida.");
        }
        catch (RegexMatchTimeoutException)
        {
            errors.Add($"{field.Label} invalido.");
        }
    }

    private static bool IsEmpty(JsonElement? value)
    {
        if (!value.HasValue || value.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return true;
        }
        if (value.Value.ValueKind == JsonValueKind.String)
        {
            return string.IsNullOrWhiteSpace(value.Value.GetString());
        }
        if (value.Value.ValueKind == JsonValueKind.Array)
        {
            return !value.Value.EnumerateArray().Any();
        }
        return false;
    }

    private static IReadOnlyList<string> Options(LeadFormField field)
    {
        return string.IsNullOrWhiteSpace(field.OptionsJson)
            ? []
            : JsonSerializer.Deserialize<IReadOnlyList<string>>(field.OptionsJson) ?? [];
    }

    private static JsonElement? ToElement(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : JsonSerializer.SerializeToElement(value);
    }

    private static JsonElement? ToElement(int value)
    {
        return value == default ? null : JsonSerializer.SerializeToElement(value);
    }

    private static int? ReadIntAnswer(IEnumerable<NormalizedLeadAnswer> answers, string key)
    {
        var answer = answers.FirstOrDefault(x => x.Field.Key == key);
        if (answer is null)
        {
            return null;
        }
        using var doc = JsonDocument.Parse(answer.ValueJson);
        return doc.RootElement.ValueKind == JsonValueKind.Number && doc.RootElement.TryGetInt32(out var value) ? value : null;
    }

    private static void ValidateMax(string? value, int max, string field, ICollection<string> erros)
    {
        if (value?.Length > max)
        {
            erros.Add($"{field} deve ter no maximo {max} caracteres.");
        }
    }

    private static TipoLead ToTipoLead(TipoContratacaoLead tipo)
    {
        return tipo switch
        {
            TipoContratacaoLead.Individual => TipoLead.PessoaFisica,
            TipoContratacaoLead.Familiar => TipoLead.Familia,
            TipoContratacaoLead.Mei => TipoLead.Mei,
            TipoContratacaoLead.Empresarial => TipoLead.Empresa,
            _ => TipoLead.PessoaFisica
        };
    }

    private LeadCaptureOptions EffectiveOptionsSync()
    {
        return EffectiveOptionsAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    private async Task<LeadCaptureOptions> EffectiveOptionsAsync(CancellationToken cancellationToken)
    {
        var current = options.Value;
        if (resolver is null)
        {
            return current;
        }
        return new LeadCaptureOptions
        {
            ConsentVersion = (await resolver.ResolveAsync(CategoriaConfiguracao.LeadCapture, "ConsentVersion", cancellationToken)).Value ?? current.ConsentVersion,
            MinimumFormSeconds = ParseInt((await resolver.ResolveAsync(CategoriaConfiguracao.LeadCapture, "MinimumFormSeconds", cancellationToken)).Value, current.MinimumFormSeconds),
            MaxLeadsPerIpPerHour = ParseInt((await resolver.ResolveAsync(CategoriaConfiguracao.LeadCapture, "MaxLeadsPerIpPerHour", cancellationToken)).Value, current.MaxLeadsPerIpPerHour),
            DuplicateWindowHours = ParseInt((await resolver.ResolveAsync(CategoriaConfiguracao.LeadCapture, "DuplicateWindowHours", cancellationToken)).Value, current.DuplicateWindowHours)
        };
    }

    private static int ParseInt(string? value, int fallback) => int.TryParse(value, out var parsed) ? parsed : fallback;

    private static string NormalizeSlug(string slug)
    {
        return CampanhaText.Slugify(slug);
    }

    private static Lead FakeLead(CapturarLeadPublicoRequest request)
    {
        return new Lead
        {
            Id = Guid.Empty,
            Nome = LeadSanitizer.Texto(request.Name ?? request.Nome, 120) ?? "Interessado",
            Cidade = LeadSanitizer.Texto(request.Cidade, 100),
            Uf = LeadSanitizer.Texto(request.Estado, 2)?.ToUpperInvariant(),
            QuantidadeVidas = request.QuantidadeVidas,
            TipoContratacao = request.TipoContratacao,
            Observacao = LeadSanitizer.Texto(request.Observacao, 1000)
        };
    }

    private sealed record NormalizedLeadForm(
        string Name,
        string Phone,
        string? Email,
        string? City,
        string? State,
        IReadOnlyList<NormalizedLeadAnswer> Answers);

    private sealed record NormalizedLeadAnswer(LeadFormField Field, string ValueJson);
}
