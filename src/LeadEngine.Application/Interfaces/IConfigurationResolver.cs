using LeadEngine.Application.DTOs;
using LeadEngine.Domain.Enums;

namespace LeadEngine.Application.Interfaces;

public interface IConfigurationResolver
{
    Task<ResolvedConfigurationValue> ResolveAsync(CategoriaConfiguracao categoria, string chave, CancellationToken cancellationToken);
    Task InvalidateAsync(CategoriaConfiguracao categoria, CancellationToken cancellationToken);
}

public sealed record ResolvedConfigurationValue(string? Value, bool Configured, OrigemConfiguracao Origem, bool Sensivel);
