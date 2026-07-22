using System.Security.Cryptography;
using System.Text;

namespace LeadEngine.Api.Security;

public static class RequestHashing
{
    public static string HashIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            return "unknown";
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(ip));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
