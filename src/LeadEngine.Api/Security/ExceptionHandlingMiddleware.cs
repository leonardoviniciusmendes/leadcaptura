using LeadEngine.Application.Common;
using LeadEngine.Infrastructure.GoogleAds;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace LeadEngine.Api.Security;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IWebHostEnvironment environment,
    GoogleAdsExceptionFormatter googleAdsExceptionFormatter)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ArgumentException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteAsync(context, "validation", ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await WriteAsync(context, "not_found", ex.Message);
        }
        catch (CampaignGenerationException ex)
        {
            logger.LogWarning(ex, "Campaign generation failed for {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status502BadGateway;
            await WriteAsync(context, "campaign_generation_failed", ex.Message);
        }
        catch (GoogleAdsDiagnosticException ex)
        {
            logger.LogError(ex, "Google Ads API error for {Path}. RequestId={RequestId}", context.Request.Path, ex.Diagnostic.RequestId);
            context.Response.StatusCode = StatusCodes.Status502BadGateway;
            var diagnostic = environment.IsDevelopment() && string.IsNullOrWhiteSpace(ex.Diagnostic.StackTrace)
                ? ex.Diagnostic with { StackTrace = ex.ToString() }
                : ex.Diagnostic;
            await WriteObjectAsync(context, diagnostic);
        }
        catch (Exception ex)
        {
            if (context.Request.Path.StartsWithSegments("/api/googleads"))
            {
                var diagnostic = googleAdsExceptionFormatter.FromException(ex);
                logger.LogError(ex, "Google Ads unhandled API error for {Path}. RequestId={RequestId}", context.Request.Path, diagnostic.RequestId);
                context.Response.StatusCode = StatusCodes.Status502BadGateway;
                await WriteObjectAsync(context, environment.IsDevelopment() && string.IsNullOrWhiteSpace(diagnostic.StackTrace)
                    ? diagnostic with { StackTrace = ex.ToString() }
                    : diagnostic);
                return;
            }

            logger.LogError(ex, "Unhandled API error for {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await WriteAsync(context, "internal_error", "Nao foi possivel processar a solicitacao.");
        }
    }

    private static Task WriteAsync(HttpContext context, string code, string message)
    {
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(new { sucesso = false, code, mensagem = message }));
    }

    private static Task WriteObjectAsync<T>(HttpContext context, T value)
    {
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }
}
