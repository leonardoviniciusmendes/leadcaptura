using System.Net;

namespace LeadEngine.Application.Common;

public sealed class MetaAdsGraphApiException(
    string message,
    string code,
    bool permissionRequired,
    HttpStatusCode? httpStatusCode = null,
    string? errorSubcode = null,
    string? type = null,
    string? fbTraceId = null) : InvalidOperationException(message)
{
    public string Code { get; } = code;
    public bool PermissionRequired { get; } = permissionRequired;
    public HttpStatusCode? HttpStatusCode { get; } = httpStatusCode;
    public string? ErrorSubcode { get; } = errorSubcode;
    public string? Type { get; } = type;
    public string? FbTraceId { get; } = fbTraceId;
}
