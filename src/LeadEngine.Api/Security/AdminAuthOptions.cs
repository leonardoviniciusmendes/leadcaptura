namespace LeadEngine.Api.Security;

public sealed class AdminAuthOptions
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}
