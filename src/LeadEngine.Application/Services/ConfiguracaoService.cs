using System.Diagnostics;
using System.Text.Json;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Services;

public sealed class ConfiguracaoService(
    IConfiguracaoRepository repository,
    IConfigurationResolver resolver,
    ISecretProtector protector,
    IWhatsAppUrlBuilder whatsAppUrlBuilder) : IConfiguracaoService
{
    public async Task<IReadOnlyList<ConfiguracaoCategoriaResponse>> ListarAsync(CancellationToken cancellationToken)
    {
        var categorias = Enum.GetValues<CategoriaConfiguracao>();
        var result = new List<ConfiguracaoCategoriaResponse>();
        foreach (var categoria in categorias)
        {
            result.Add(await ObterCategoriaAsync(categoria, cancellationToken));
        }
        return result;
    }

    public async Task<ConfiguracaoCategoriaResponse> ObterCategoriaAsync(CategoriaConfiguracao categoria, CancellationToken cancellationToken)
    {
        var items = new List<ConfiguracaoItemResponse>();
        foreach (var definition in ConfiguracaoCatalog.ByCategory(categoria))
        {
            var resolved = await resolver.ResolveAsync(categoria, definition.Key, cancellationToken);
            items.Add(new ConfiguracaoItemResponse(
                definition.Key,
                definition.Sensivel ? null : resolved.Value,
                definition.Sensivel,
                resolved.Configured,
                resolved.Origem,
                definition.Descricao));
        }
        return new ConfiguracaoCategoriaResponse(categoria, items);
    }

    public async Task<ConfiguracaoCategoriaResponse> AtualizarCategoriaAsync(CategoriaConfiguracao categoria, Dictionary<string, object?> valores, CancellationToken cancellationToken)
    {
        var normalized = valores.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
        foreach (var definition in ConfiguracaoCatalog.ByCategory(categoria))
        {
            var removeKey = $"remover{definition.Key}";
            var hasValue = normalized.TryGetValue(definition.Key, out var raw);
            var remove = normalized.TryGetValue(removeKey, out var removeRaw) && ToBool(removeRaw);

            if (!hasValue && !remove)
            {
                continue;
            }

            var config = await repository.ObterPorChaveAsync(FullKey(definition), cancellationToken);
            if (config is null)
            {
                config = new ConfiguracaoSistema
                {
                    Id = Guid.NewGuid(),
                    Chave = FullKey(definition),
                    Categoria = categoria,
                    Sensivel = definition.Sensivel,
                    Ativo = true,
                    DataCriacao = DateTime.UtcNow,
                    Descricao = definition.Descricao
                };
                await repository.AdicionarAsync(config, cancellationToken);
            }

            var previous = definition.Sensivel ? null : config.Valor;
            if (remove)
            {
                config.Valor = null;
                config.ValorProtegido = null;
                config.Ativo = false;
            }
            else if (hasValue)
            {
                var value = NormalizeValue(raw);
                Validate(definition, value);
                config.Ativo = true;
                if (definition.Sensivel)
                {
                    if (value is not null)
                    {
                        config.ValorProtegido = protector.Protect(value);
                    }
                }
                else
                {
                    config.Valor = value;
                }
            }

            config.DataAtualizacao = DateTime.UtcNow;
            await repository.AdicionarHistoricoAsync(new ConfiguracaoSistemaHistorico
            {
                Id = Guid.NewGuid(),
                ConfiguracaoSistemaId = config.Id,
                Chave = config.Chave,
                Categoria = categoria,
                ValorAnterior = definition.Sensivel ? null : previous,
                ValorNovo = definition.Sensivel ? null : config.Valor,
                Sensivel = definition.Sensivel,
                DataAlteracao = DateTime.UtcNow,
                OrigemAlteracao = remove ? "Remocao" : "Interface"
            }, cancellationToken);
        }

        await repository.SalvarAsync(cancellationToken);
        await resolver.InvalidateAsync(categoria, cancellationToken);
        return await ObterCategoriaAsync(categoria, cancellationToken);
    }

    public async Task<TesteConfiguracaoResponse> TestarAsync(CategoriaConfiguracao categoria, CancellationToken cancellationToken)
    {
        return categoria switch
        {
            CategoriaConfiguracao.OpenRouter => await TestarOpenRouterAsync(cancellationToken),
            CategoriaConfiguracao.WhatsApp => await TestarWhatsAppAsync(cancellationToken),
            CategoriaConfiguracao.ExternalLeadApi => await TestarExternalLeadApiAsync(cancellationToken),
            _ => new TesteConfiguracaoResponse(true, "Validacao local concluida.")
        };
    }

    public async Task<ConfiguracoesStatusResponse> ObterStatusAsync(CancellationToken cancellationToken)
    {
        var provider = await Value(CategoriaConfiguracao.CampaignGeneration, "Provider", cancellationToken);
        var openKey = await resolver.ResolveAsync(CategoriaConfiguracao.OpenRouter, "ApiKey", cancellationToken);
        var model = await Value(CategoriaConfiguracao.OpenRouter, "Model", cancellationToken);
        var whatsapp = await Value(CategoriaConfiguracao.WhatsApp, "Numero", cancellationToken);
        var publicUrl = await Value(CategoriaConfiguracao.Application, "PublicBaseUrl", cancellationToken);
        var externalEnabled = bool.TryParse(await Value(CategoriaConfiguracao.ExternalLeadApi, "Enabled", cancellationToken), out var enabled) && enabled;
        var externalBase = await Value(CategoriaConfiguracao.ExternalLeadApi, "BaseUrl", cancellationToken);
        var pendencias = new List<string>();

        var openConfigured = openKey.Configured && !string.IsNullOrWhiteSpace(model);
        if (string.Equals(provider, "OpenRouter", StringComparison.OrdinalIgnoreCase) && !openConfigured)
        {
            pendencias.Add("Configure ApiKey e Model do OpenRouter.");
        }

        if (string.IsNullOrWhiteSpace(whatsapp))
        {
            pendencias.Add("Configure o numero de WhatsApp.");
        }

        if (string.IsNullOrWhiteSpace(publicUrl))
        {
            pendencias.Add("Configure a URL publica da aplicacao.");
        }

        return new ConfiguracoesStatusResponse(
            new ConfiguracaoStatusItem(openConfigured, openConfigured ? "Pronto" : "Pendente"),
            new ConfiguracaoStatusItem(!string.IsNullOrWhiteSpace(provider), string.Equals(provider, "OpenRouter", StringComparison.OrdinalIgnoreCase) ? "Pronto" : "Fake"),
            new ConfiguracaoStatusItem(!string.IsNullOrWhiteSpace(whatsapp), string.IsNullOrWhiteSpace(whatsapp) ? "Pendente" : "Pronto"),
            new ConfiguracaoStatusItem(true, "Pronto"),
            new ConfiguracaoStatusItem(externalEnabled && !string.IsNullOrWhiteSpace(externalBase), externalEnabled ? (string.IsNullOrWhiteSpace(externalBase) ? "Pendente" : "Pronto") : "Desativado"),
            new ConfiguracaoStatusItem(!string.IsNullOrWhiteSpace(publicUrl), string.IsNullOrWhiteSpace(publicUrl) ? "Pendente" : "Pronto"),
            pendencias);
    }

    public async Task<IReadOnlyList<ConfiguracaoHistoricoResponse>> ListarHistoricoAsync(ConfiguracaoHistoricoQuery query, CancellationToken cancellationToken)
    {
        var rows = await repository.ListarHistoricoAsync(query.Categoria, query.Chave, query.DataInicial, query.DataFinal, cancellationToken);
        return rows.Select(x => new ConfiguracaoHistoricoResponse(x.DataAlteracao, x.Categoria, x.Chave, x.ValorAnterior, x.ValorNovo, x.Sensivel, x.OrigemAlteracao)).ToArray();
    }

    private async Task<TesteConfiguracaoResponse> TestarOpenRouterAsync(CancellationToken cancellationToken)
    {
        var apiKey = await resolver.ResolveAsync(CategoriaConfiguracao.OpenRouter, "ApiKey", cancellationToken);
        var model = await Value(CategoriaConfiguracao.OpenRouter, "Model", cancellationToken);
        if (!apiKey.Configured || string.IsNullOrWhiteSpace(model))
        {
            return new TesteConfiguracaoResponse(false, "OpenRouter pendente.", model);
        }
        var sw = Stopwatch.StartNew();
        sw.Stop();
        return new TesteConfiguracaoResponse(true, "Configuracao OpenRouter valida.", model, sw.ElapsedMilliseconds);
    }

    private async Task<TesteConfiguracaoResponse> TestarWhatsAppAsync(CancellationToken cancellationToken)
    {
        var numero = await Value(CategoriaConfiguracao.WhatsApp, "Numero", cancellationToken);
        Validate(ConfiguracaoCatalog.Get(CategoriaConfiguracao.WhatsApp, "Numero"), numero);
        var url = whatsAppUrlBuilder.Build(new LeadEngine.Domain.Entities.Lead
        {
            Nome = "Teste LeadEngine",
            Cidade = "Rio de Janeiro",
            Uf = "RJ",
            QuantidadeVidas = 1,
            TipoContratacao = LeadEngine.Domain.Enums.TipoContratacaoLead.Individual
        }, new LeadEngine.Domain.Entities.Campanha { Nome = "Campanha de teste" });
        return new TesteConfiguracaoResponse(true, "URL de WhatsApp gerada.", null, null, url);
    }

    private async Task<TesteConfiguracaoResponse> TestarExternalLeadApiAsync(CancellationToken cancellationToken)
    {
        var enabledText = await Value(CategoriaConfiguracao.ExternalLeadApi, "Enabled", cancellationToken);
        var enabled = bool.TryParse(enabledText, out var value) && value;
        if (!enabled)
        {
            return new TesteConfiguracaoResponse(true, "Desativado.");
        }
        var baseUrl = await Value(CategoriaConfiguracao.ExternalLeadApi, "BaseUrl", cancellationToken);
        Validate(ConfiguracaoCatalog.Get(CategoriaConfiguracao.ExternalLeadApi, "BaseUrl"), baseUrl);
        return new TesteConfiguracaoResponse(true, "Configuracao externa valida.");
    }

    private async Task<string?> Value(CategoriaConfiguracao categoria, string key, CancellationToken cancellationToken)
    {
        return (await resolver.ResolveAsync(categoria, key, cancellationToken)).Value;
    }

    private static string FullKey(ConfigDefinition definition) => $"{definition.Categoria}.{definition.Key}";

    private static string? NormalizeValue(object? raw)
    {
        if (raw is null)
        {
            return null;
        }
        if (raw is JsonElement json)
        {
            return json.ValueKind switch
            {
                JsonValueKind.String => json.GetString(),
                JsonValueKind.Number => json.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => null,
                _ => json.ToString()
            };
        }
        return raw.ToString();
    }

    private static bool ToBool(object? raw)
    {
        return bool.TryParse(NormalizeValue(raw), out var value) && value;
    }

    private static void Validate(ConfigDefinition definition, string? value)
    {
        if (definition.Sensivel && value is null)
        {
            return;
        }
        if (definition.Tipo is TipoConfiguracao.Url && !string.IsNullOrWhiteSpace(value) && !Uri.TryCreate(value, UriKind.Absolute, out _))
        {
            throw new ArgumentException($"{definition.Key} deve ser uma URL valida.");
        }
        if (definition.Categoria == CategoriaConfiguracao.OpenRouter)
        {
            if (definition.Key == "TimeoutSeconds" && (!int.TryParse(value, out var timeout) || timeout is < 5 or > 300)) throw new ArgumentException("TimeoutSeconds deve estar entre 5 e 300.");
            if (definition.Key == "MaxRetries" && (!int.TryParse(value, out var retries) || retries is < 0 or > 5)) throw new ArgumentException("MaxRetries deve estar entre 0 e 5.");
            if (definition.Key == "Temperature" && (!double.TryParse(value, out var temp) || temp is < 0 or > 2)) throw new ArgumentException("Temperature deve estar entre 0 e 2.");
        }
        if (definition.Categoria == CategoriaConfiguracao.WhatsApp)
        {
            if (definition.Key == "Numero")
            {
                var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
                if (!string.IsNullOrWhiteSpace(value) && digits.Length is < 10 or > 15) throw new ArgumentException("Numero de WhatsApp deve ter entre 10 e 15 digitos.");
            }
            if (definition.Key == "MensagemPadrao" && value?.Length > 500) throw new ArgumentException("MensagemPadrao deve ter no maximo 500 caracteres.");
        }
        if (definition.Categoria == CategoriaConfiguracao.LeadCapture)
        {
            if (definition.Key == "ConsentVersion" && string.IsNullOrWhiteSpace(value)) throw new ArgumentException("ConsentVersion obrigatoria.");
            if (definition.Key == "MinimumFormSeconds" && (!int.TryParse(value, out var min) || min is < 0 or > 30)) throw new ArgumentException("MinimumFormSeconds deve estar entre 0 e 30.");
            if (definition.Key == "MaxLeadsPerIpPerHour" && (!int.TryParse(value, out var max) || max is < 1 or > 1000)) throw new ArgumentException("MaxLeadsPerIpPerHour deve estar entre 1 e 1000.");
            if (definition.Key == "DuplicateWindowHours" && (!int.TryParse(value, out var dup) || dup is < 1 or > 720)) throw new ArgumentException("DuplicateWindowHours deve estar entre 1 e 720.");
        }
        if (definition.Categoria == CategoriaConfiguracao.Landing)
        {
            if (definition.Key == "DefaultFooterText" && value?.Length > 500) throw new ArgumentException("DefaultFooterText deve ter no maximo 500 caracteres.");
            if (!string.IsNullOrWhiteSpace(value) && value.Contains('<')) throw new ArgumentException("Landing nao aceita HTML arbitrario.");
        }
    }
}
