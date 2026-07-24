using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace LeadEngine.Infrastructure.Configuration;

public sealed class ConfigurationResolver(
    IConfiguracaoRepository repository,
    ISecretProtector protector,
    IConfiguration configuration,
    IMemoryCache cache) : IConfigurationResolver
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<ResolvedConfigurationValue> ResolveAsync(CategoriaConfiguracao categoria, string chave, CancellationToken cancellationToken)
    {
        var definition = ConfiguracaoCatalog.Get(categoria, chave);
        var cacheKey = $"cfg:{categoria}:{definition.Key}";
        if (cache.TryGetValue<ResolvedConfigurationValue>(cacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        var fullKey = $"{definition.Categoria}.{definition.Key}";
        var saved = await repository.ObterPorChaveAsync(fullKey, cancellationToken);
        if (saved is { Ativo: true })
        {
            var value = definition.Sensivel
                ? (string.IsNullOrWhiteSpace(saved.ValorProtegido) ? null : protector.Unprotect(saved.ValorProtegido))
                : saved.Valor;
            var result = new ResolvedConfigurationValue(value, !string.IsNullOrWhiteSpace(value), OrigemConfiguracao.Banco, definition.Sensivel);
            cache.Set(cacheKey, result, CacheTtl);
            return result;
        }

        var env = Environment.GetEnvironmentVariable(definition.EnvName);
        if (!string.IsNullOrWhiteSpace(env))
        {
            var result = new ResolvedConfigurationValue(env, true, OrigemConfiguracao.VariavelAmbiente, definition.Sensivel);
            cache.Set(cacheKey, result, CacheTtl);
            return result;
        }

        var appValue = configuration[definition.AppSettingsPath];
        if (!string.IsNullOrWhiteSpace(appValue))
        {
            var result = new ResolvedConfigurationValue(appValue, true, OrigemConfiguracao.AppSettings, definition.Sensivel);
            cache.Set(cacheKey, result, CacheTtl);
            return result;
        }

        var fallback = definition.DefaultValue;
        var fallbackResult = new ResolvedConfigurationValue(fallback, !string.IsNullOrWhiteSpace(fallback), OrigemConfiguracao.Padrao, definition.Sensivel);
        cache.Set(cacheKey, fallbackResult, CacheTtl);
        return fallbackResult;
    }

    public Task InvalidateAsync(CategoriaConfiguracao categoria, CancellationToken cancellationToken)
    {
        foreach (var item in ConfiguracaoCatalog.ByCategory(categoria))
        {
            cache.Remove($"cfg:{categoria}:{item.Key}");
        }
        return Task.CompletedTask;
    }
}
