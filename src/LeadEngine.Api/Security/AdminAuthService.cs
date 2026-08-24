using Microsoft.Extensions.Options;

namespace LeadEngine.Api.Security;

public sealed class AdminAuthService(IOptions<AdminAuthOptions> options)
{
    public bool Validate(string email, string password)
    {
        var config = options.Value;
        return !string.IsNullOrWhiteSpace(config.Email)
            && !string.IsNullOrWhiteSpace(config.PasswordHash)
            && string.Equals(email.Trim(), config.Email.Trim(), StringComparison.OrdinalIgnoreCase)
            && PasswordHasher.Verify(password, config.PasswordHash);
    }
}
