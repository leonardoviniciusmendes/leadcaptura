using System.Text.Json;
using LeadEngine.Application.DTOs;
using LeadEngine.Domain.Entities;

namespace LeadEngine.Application.Services;

public static class LeadFormSchema
{
    public static readonly ISet<string> SupportedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "text",
        "phone",
        "email",
        "number",
        "select",
        "multiselect",
        "radio",
        "checkbox",
        "textarea",
        "date"
    };

    public static LeadForm CreateForCampaign(Campanha campanha)
    {
        var config = ReadConfiguredForm(campanha.Segment?.DefaultConfigJson);
        var form = new LeadForm
        {
            Id = Guid.NewGuid(),
            CampaignId = campanha.Id,
            Campaign = campanha,
            Version = 1,
            SubmitButtonText = config.SubmitButtonText,
            IsActive = true
        };

        form.Fields = config.Fields.Select(field => new LeadFormField
        {
            Id = Guid.NewGuid(),
            LeadFormId = form.Id,
            LeadForm = form,
            Key = field.Key,
            Label = field.Label,
            Type = field.Type,
            Required = field.Required,
            Placeholder = field.Placeholder,
            OptionsJson = field.Options.Count == 0 ? null : JsonSerializer.Serialize(field.Options),
            ValidationJson = field.ValidationJson,
            Order = field.Order,
            DefaultValue = field.DefaultValue
        }).ToArray();

        return form;
    }

    public static LeadForm GetEffectiveForm(Campanha campanha)
    {
        var existing = campanha.LeadForms
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.Version)
            .FirstOrDefault();

        if (existing is not null)
        {
            return existing;
        }

        return CreateForCampaign(campanha);
    }

    public static LeadForm CreateRevision(Campanha campanha, LeadFormResponse request)
    {
        if (request.Fields.Count == 0)
        {
            throw new ArgumentException("Formulario deve possuir ao menos um campo.");
        }

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var version = campanha.LeadForms.Count == 0 ? 1 : campanha.LeadForms.Max(x => x.Version) + 1;
        var form = new LeadForm
        {
            Id = Guid.NewGuid(),
            CampaignId = campanha.Id,
            Campaign = campanha,
            Version = version,
            SubmitButtonText = string.IsNullOrWhiteSpace(request.SubmitButtonText) ? "Enviar" : request.SubmitButtonText.Trim(),
            IsActive = true
        };

        var fields = new List<LeadFormField>();
        foreach (var item in request.Fields)
        {
            if (string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Label))
            {
                throw new ArgumentException("Campos do formulario devem possuir key e label.");
            }
            if (!SupportedTypes.Contains(item.Type))
            {
                throw new ArgumentException($"Tipo de campo invalido: {item.Type}.");
            }
            if (!keys.Add(item.Key.Trim()))
            {
                throw new ArgumentException($"Campo duplicado no formulario: {item.Key}.");
            }

            fields.Add(new LeadFormField
            {
                Id = Guid.NewGuid(),
                LeadFormId = form.Id,
                LeadForm = form,
                Key = item.Key.Trim(),
                Label = item.Label.Trim(),
                Type = item.Type.Trim().ToLowerInvariant(),
                Required = item.Required,
                Placeholder = string.IsNullOrWhiteSpace(item.Placeholder) ? null : item.Placeholder.Trim(),
                OptionsJson = item.Options.Count == 0 ? null : JsonSerializer.Serialize(item.Options.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()),
                Order = fields.Count + 1,
                DefaultValue = string.IsNullOrWhiteSpace(item.DefaultValue) ? null : item.DefaultValue.Trim()
            });
        }

        form.Fields = fields;
        return form;
    }

    private static FormConfig ReadConfiguredForm(string? defaultConfigJson)
    {
        if (!string.IsNullOrWhiteSpace(defaultConfigJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(defaultConfigJson);
                if (doc.RootElement.TryGetProperty("leadForm", out var formElement))
                {
                    var parsed = ParseForm(formElement);
                    if (parsed.Fields.Count > 0)
                    {
                        return parsed;
                    }
                }
            }
            catch (JsonException)
            {
                // Segment JSON is validated elsewhere. For presentation fallback, use a safe universal schema.
            }
        }

        return UniversalFallback();
    }

    private static FormConfig ParseForm(JsonElement element)
    {
        var submit = ReadString(element, "submitButtonText") ?? "Enviar";
        var fields = new List<FieldConfig>();
        if (!element.TryGetProperty("fields", out var fieldsElement) || fieldsElement.ValueKind != JsonValueKind.Array)
        {
            return new FormConfig(submit, []);
        }

        foreach (var field in fieldsElement.EnumerateArray())
        {
            var key = ReadString(field, "key");
            var label = ReadString(field, "label");
            var type = ReadString(field, "type")?.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(type) || !SupportedTypes.Contains(type))
            {
                continue;
            }

            fields.Add(new FieldConfig(
                key.Trim(),
                label.Trim(),
                type,
                ReadBool(field, "required"),
                ReadString(field, "placeholder"),
                ReadOptions(field),
                ReadObjectRaw(field, "validationJson") ?? ReadObjectRaw(field, "validation"),
                ReadInt(field, "order") ?? fields.Count + 1,
                ReadString(field, "defaultValue")));
        }

        return new FormConfig(submit, fields.OrderBy(x => x.Order).ToArray());
    }

    private static FormConfig UniversalFallback()
    {
        return new FormConfig("Enviar", [
            new FieldConfig("name", "Nome", "text", true, null, [], null, 1, null),
            new FieldConfig("phone", "WhatsApp", "phone", true, "(00) 00000-0000", [], null, 2, null),
            new FieldConfig("email", "Email", "email", false, null, [], null, 3, null)
        ]);
    }

    private static string? ReadString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static bool ReadBool(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.True;
    }

    private static int? ReadInt(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)
            ? number
            : null;
    }

    private static IReadOnlyList<string> ReadOptions(JsonElement element)
    {
        if (!element.TryGetProperty("options", out var options) || options.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return options.EnumerateArray()
            .Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() : null)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToArray();
    }

    private static string? ReadObjectRaw(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind is JsonValueKind.Object
            ? value.GetRawText()
            : null;
    }

    private sealed record FormConfig(string SubmitButtonText, IReadOnlyList<FieldConfig> Fields);

    private sealed record FieldConfig(
        string Key,
        string Label,
        string Type,
        bool Required,
        string? Placeholder,
        IReadOnlyList<string> Options,
        string? ValidationJson,
        int Order,
        string? DefaultValue);
}
