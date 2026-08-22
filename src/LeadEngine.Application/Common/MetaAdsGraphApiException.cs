using System.Net;

namespace LeadEngine.Application.Common;

public sealed class MetaAdsGraphApiException(
    string message,
    string code,
    bool permissionRequired,
    HttpStatusCode? httpStatusCode = null,
    string? errorSubcode = null,
    string? type = null,
    string? fbTraceId = null,
    string? metaMessage = null,
    string? errorUserTitle = null,
    string? errorUserMessage = null,
    string? errorData = null,
    string? blameField = null,
    string? blameFieldSpecs = null,
    bool? isTransient = null) : InvalidOperationException(message)
{
    public string Code { get; } = code;
    public bool PermissionRequired { get; } = permissionRequired;
    public HttpStatusCode? HttpStatusCode { get; } = httpStatusCode;
    public string? ErrorSubcode { get; } = errorSubcode;
    public string? Type { get; } = type;
    public string? FbTraceId { get; } = fbTraceId;
    public string? MetaMessage { get; } = metaMessage;
    public string? ErrorUserTitle { get; } = errorUserTitle;
    public string? ErrorUserMessage { get; } = errorUserMessage;
    public string? ErrorData { get; } = errorData;
    public string? BlameField { get; } = blameField;
    public string? BlameFieldSpecs { get; } = blameFieldSpecs;
    public bool? IsTransient { get; } = isTransient;
}
