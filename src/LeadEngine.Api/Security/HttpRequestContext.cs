using LeadEngine.Application.Interfaces;

namespace LeadEngine.Api.Security;

public sealed class HttpRequestContext(IHttpContextAccessor accessor) : IRequestContext
{
    public string? IpHash => RequestHashing.HashIp(accessor.HttpContext?.Connection.RemoteIpAddress?.ToString());
    public string? UserAgent => accessor.HttpContext?.Request.Headers.UserAgent.ToString();
}
