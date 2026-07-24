using LeadEngine.Application.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace LeadEngine.Infrastructure.Configuration;

public sealed class DataProtectionSecretProtector(IDataProtectionProvider provider) : ISecretProtector
{
    private readonly IDataProtector protector = provider.CreateProtector("LeadEngine.Configuracoes.Segredos");

    public string Protect(string value) => protector.Protect(value);

    public string Unprotect(string protectedValue) => protector.Unprotect(protectedValue);
}
