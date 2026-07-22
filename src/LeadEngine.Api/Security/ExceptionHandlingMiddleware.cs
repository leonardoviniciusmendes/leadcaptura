using System.Text.Json;

namespace LeadEngine.Api.Security;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
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
}
