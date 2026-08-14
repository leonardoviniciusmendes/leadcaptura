using System.Text.Json;
using Grpc.Core;
using LeadEngine.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace LeadEngine.Infrastructure.GoogleAds;

public sealed class GoogleAdsExceptionFormatter(ILogger<GoogleAdsExceptionFormatter> logger)
{
    public GoogleAdsDiagnosticResponse FromRestError(string body, string? requestId, string? statusCode = null, string? detail = null)
    {
        var errors = ParseRestErrors(body, requestId, statusCode, detail);
        var code = errors.FirstOrDefault()?.Codigo ?? statusCode ?? "google_ads_error";
        var message = errors.FirstOrDefault()?.Mensagem ?? detail ?? "Google Ads rejeitou a operacao.";
        var responseRequestId = requestId ?? ExtractRequestId(body);
        LogDiagnostic("REST", responseRequestId, body, null);
        return new GoogleAdsDiagnosticResponse(false, code, message, responseRequestId, errors, statusCode, detail);
    }

    public GoogleAdsDiagnosticResponse FromException(Exception exception, string? requestId = null)
    {
        if (exception is AggregateException aggregate)
        {
            var diagnostics = aggregate.InnerExceptions.Select(x => FromException(x, requestId)).ToArray();
            var errors = diagnostics.SelectMany(x => x.Erros).ToArray();
            var first = diagnostics.FirstOrDefault();
            LogDiagnostic("AggregateException", first?.RequestId ?? requestId, null, exception);
            return new GoogleAdsDiagnosticResponse(
                false,
                first?.Codigo ?? "aggregate_exception",
                first?.Mensagem ?? "Falha agregada ao chamar Google Ads.",
                first?.RequestId ?? requestId,
                errors.Length > 0 ? errors : [GenericError("aggregate_exception", exception.Message, requestId)],
                first?.StatusCode,
                first?.Detail,
                DevelopmentStack(exception));
        }

        if (exception.GetType().Name == "GoogleAdsException")
        {
            var responseRequestId = requestId ?? exception.GetType().GetProperty("RequestId")?.GetValue(exception)?.ToString();
            var failure = exception.GetType().GetProperty("Failure")?.GetValue(exception);
            var errors = ExtractSdkErrors(failure, responseRequestId);

            LogDiagnostic("GoogleAdsException", responseRequestId, failure?.ToString(), exception);
            return new GoogleAdsDiagnosticResponse(
                false,
                errors.FirstOrDefault()?.Codigo ?? "google_ads_error",
                errors.FirstOrDefault()?.Mensagem ?? "Google Ads rejeitou a operacao.",
                responseRequestId,
                errors.Length > 0 ? errors : [GenericError("google_ads_error", exception.Message, responseRequestId)],
                StackTrace: DevelopmentStack(exception));
        }

        if (exception is RpcException rpcException)
        {
            var error = new GoogleAdsPublicationErrorDto(
                rpcException.StatusCode.ToString(),
                string.IsNullOrWhiteSpace(rpcException.Status.Detail) ? "Erro RPC ao chamar Google Ads." : rpcException.Status.Detail,
                null,
                null,
                null,
                null,
                requestId,
                IsRecoverable(rpcException.StatusCode.ToString()),
                Suggested(rpcException.StatusCode.ToString()),
                StatusCode: rpcException.StatusCode.ToString(),
                Detail: rpcException.Status.Detail);
            LogDiagnostic("RpcException", requestId, rpcException.Status.Detail, exception);
            return new GoogleAdsDiagnosticResponse(false, error.Codigo, error.Mensagem, requestId, [error], rpcException.StatusCode.ToString(), rpcException.Status.Detail, DevelopmentStack(exception));
        }

        LogDiagnostic(exception.GetType().Name, requestId, exception.InnerException?.ToString(), exception);
        return new GoogleAdsDiagnosticResponse(
            false,
            "google_ads_error",
            string.IsNullOrWhiteSpace(exception.Message) ? "Falha ao chamar Google Ads." : exception.Message,
            requestId,
            [GenericError("google_ads_error", exception.Message, requestId)],
            StackTrace: DevelopmentStack(exception));
    }

    private void LogDiagnostic(string source, string? requestId, string? googleAdsFailure, Exception? exception)
    {
        logger.LogError(
            exception,
            "Google Ads diagnostic failure. Source={Source}; RequestId={RequestId}; GoogleAdsFailure={GoogleAdsFailure}; InnerException={InnerException}",
            source,
            requestId,
            googleAdsFailure,
            exception?.InnerException?.ToString());
    }

    private static IReadOnlyList<GoogleAdsPublicationErrorDto> ParseRestErrors(string body, string? requestId, string? statusCode, string? detail)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            var root = doc.RootElement;
            var responseRequestId = requestId ?? ExtractRequestId(root);
            var result = new List<GoogleAdsPublicationErrorDto>();
            if (root.TryGetProperty("error", out var errorRoot))
            {
                var topMessage = S(errorRoot, "message") ?? detail ?? "Google Ads rejeitou a operacao.";
                var topStatus = S(errorRoot, "status") ?? statusCode ?? "google_ads_error";
                if (errorRoot.TryGetProperty("details", out var details) && details.ValueKind == JsonValueKind.Array)
                {
                    foreach (var detailItem in details.EnumerateArray())
                    {
                        if (detailItem.TryGetProperty("requestId", out var detailRequestId) && detailRequestId.ValueKind == JsonValueKind.String)
                        {
                            responseRequestId ??= detailRequestId.GetString();
                        }

                        if (detailItem.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in errors.EnumerateArray())
                            {
                                var fieldPath = FieldPath(item);
                                var location = fieldPath.Length == 0 ? null : string.Join(".", fieldPath);
                                var code = ErrorCode(item) ?? topStatus;
                                result.Add(new GoogleAdsPublicationErrorDto(
                                    code,
                                    S(item, "message") ?? topMessage,
                                    OperationFromFieldPath(fieldPath),
                                    IndexFromFieldPath(fieldPath),
                                    location,
                                    null,
                                    responseRequestId,
                                    IsRecoverable(code),
                                    Suggested(code),
                                    location,
                                    fieldPath,
                                    Trigger(item),
                                    statusCode,
                                    detail));
                            }
                        }
                    }
                }

                if (result.Count == 0)
                {
                    result.Add(new GoogleAdsPublicationErrorDto(topStatus, topMessage, null, null, null, null, responseRequestId, IsRecoverable(topStatus), Suggested(topStatus), StatusCode: statusCode, Detail: detail));
                }
            }

            return result;
        }
        catch (JsonException)
        {
            return [new GoogleAdsPublicationErrorDto(statusCode ?? "google_ads_error", detail ?? "Google Ads rejeitou a operacao.", null, null, null, null, requestId, true, "Consulte o requestId no log.", StatusCode: statusCode, Detail: detail)];
        }
    }

    private static string? ExtractRequestId(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            return ExtractRequestId(doc.RootElement);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ExtractRequestId(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("requestId", out var requestId) && requestId.ValueKind == JsonValueKind.String)
            {
                return requestId.GetString();
            }

            foreach (var property in element.EnumerateObject())
            {
                var found = ExtractRequestId(property.Value);
                if (!string.IsNullOrWhiteSpace(found))
                {
                    return found;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var found = ExtractRequestId(item);
                if (!string.IsNullOrWhiteSpace(found))
                {
                    return found;
                }
            }
        }

        return null;
    }

    private static string[] FieldPath(JsonElement item)
    {
        if (!item.TryGetProperty("location", out var location) ||
            !location.TryGetProperty("fieldPathElements", out var elements) ||
            elements.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return elements.EnumerateArray().Select(FieldPathElement).Where(x => x.Length > 0).ToArray();
    }

    private static string FieldPathElement(JsonElement element)
    {
        var field = S(element, "fieldName") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(field))
        {
            return string.Empty;
        }

        return element.TryGetProperty("index", out var index) && index.ValueKind == JsonValueKind.Number
            ? $"{field}[{index.GetInt32()}]"
            : field;
    }

    private static GoogleAdsPublicationErrorDto[] ExtractSdkErrors(object? failure, string? requestId)
    {
        var errorsObject = failure?.GetType().GetProperty("Errors")?.GetValue(failure) as System.Collections.IEnumerable;
        if (errorsObject is null)
        {
            return [];
        }

        var result = new List<GoogleAdsPublicationErrorDto>();
        foreach (var error in errorsObject)
        {
            if (error is null)
            {
                continue;
            }

            var errorCode = error.GetType().GetProperty("ErrorCode")?.GetValue(error)?.ToString() ?? "google_ads_error";
            var message = error.GetType().GetProperty("Message")?.GetValue(error)?.ToString() ?? "Erro retornado pelo Google Ads.";
            var trigger = error.GetType().GetProperty("Trigger")?.GetValue(error)?.ToString();
            var fieldPath = ExtractSdkFieldPath(error.GetType().GetProperty("Location")?.GetValue(error));
            var location = fieldPath.Length == 0 ? null : string.Join(".", fieldPath);
            result.Add(new GoogleAdsPublicationErrorDto(
                errorCode,
                message,
                OperationFromFieldPath(fieldPath),
                IndexFromFieldPath(fieldPath),
                location,
                null,
                requestId,
                IsRecoverable(errorCode),
                Suggested(errorCode),
                location,
                fieldPath,
                trigger));
        }

        return result.ToArray();
    }

    private static string[] ExtractSdkFieldPath(object? location)
    {
        var elements = location?.GetType().GetProperty("FieldPathElements")?.GetValue(location) as System.Collections.IEnumerable;
        if (elements is null)
        {
            return [];
        }

        var result = new List<string>();
        foreach (var element in elements)
        {
            if (element is null)
            {
                continue;
            }

            var field = element.GetType().GetProperty("FieldName")?.GetValue(element)?.ToString();
            if (string.IsNullOrWhiteSpace(field))
            {
                continue;
            }

            var index = element.GetType().GetProperty("Index")?.GetValue(element);
            result.Add(index is null ? field : $"{field}[{index}]");
        }

        return result.ToArray();
    }

    private static string ErrorCode(JsonElement item)
    {
        if (!item.TryGetProperty("errorCode", out var errorCode) || errorCode.ValueKind != JsonValueKind.Object)
        {
            return "google_ads_error";
        }

        foreach (var property in errorCode.EnumerateObject())
        {
            var value = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : property.Value.ToString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return $"{property.Name}.{value}";
            }
        }

        return "google_ads_error";
    }

    private static string? Trigger(JsonElement item)
    {
        if (!item.TryGetProperty("trigger", out var trigger))
        {
            return null;
        }

        if (trigger.ValueKind == JsonValueKind.String)
        {
            return trigger.GetString();
        }

        if (trigger.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in trigger.EnumerateObject())
            {
                var value = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : property.Value.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }

        return trigger.ToString();
    }

    private static string? S(JsonElement e, string p) => e.TryGetProperty(p, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    private static string? DevelopmentStack(Exception exception) => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" ? exception.ToString() : null;

    private static string? OperationFromFieldPath(IReadOnlyList<string> fieldPath)
    {
        return fieldPath.FirstOrDefault(x => x.Contains("Operation", StringComparison.OrdinalIgnoreCase) || x.Contains("operations", StringComparison.OrdinalIgnoreCase));
    }

    private static int? IndexFromFieldPath(IReadOnlyList<string> fieldPath)
    {
        foreach (var item in fieldPath)
        {
            var start = item.IndexOf('[', StringComparison.Ordinal);
            var end = item.IndexOf(']', StringComparison.Ordinal);
            if (start >= 0 && end > start && int.TryParse(item[(start + 1)..end], out var index))
            {
                return index;
            }
        }

        return null;
    }

    private static GoogleAdsPublicationErrorDto GenericError(string code, string message, string? requestId)
    {
        return new GoogleAdsPublicationErrorDto(code, string.IsNullOrWhiteSpace(message) ? "Falha ao chamar Google Ads." : message, null, null, null, null, requestId, IsRecoverable(code), Suggested(code));
    }

    private static bool IsRecoverable(string code)
    {
        return code.Contains("quota", StringComparison.OrdinalIgnoreCase)
            || code.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || code.Contains("unavailable", StringComparison.OrdinalIgnoreCase)
            || code.Contains("unauthenticated", StringComparison.OrdinalIgnoreCase)
            || code.Contains("authentication", StringComparison.OrdinalIgnoreCase);
    }

    private static string Suggested(string code)
    {
        if (code.Contains("auth", StringComparison.OrdinalIgnoreCase) || code.Contains("unauthenticated", StringComparison.OrdinalIgnoreCase)) return "Reconecte a conta Google Ads.";
        if (code.Contains("permission", StringComparison.OrdinalIgnoreCase) || code.Contains("access", StringComparison.OrdinalIgnoreCase)) return "Verifique permissoes do customer e developer token.";
        if (code.Contains("budget", StringComparison.OrdinalIgnoreCase)) return "Ajuste o orcamento no preview.";
        if (code.Contains("url", StringComparison.OrdinalIgnoreCase)) return "Valide a URL final publicada.";
        if (code.Contains("policy", StringComparison.OrdinalIgnoreCase)) return "Revise headlines e descriptions.";
        if (code.Contains("keyword", StringComparison.OrdinalIgnoreCase)) return "Revise as keywords do preview.";
        if (code.Contains("quota", StringComparison.OrdinalIgnoreCase)) return "Tente novamente mais tarde.";
        if (code.Contains("timeout", StringComparison.OrdinalIgnoreCase) || code.Contains("unavailable", StringComparison.OrdinalIgnoreCase)) return "Tente novamente.";
        return "Consulte o requestId e revise a configuracao.";
    }
}
