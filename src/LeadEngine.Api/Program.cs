using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using LeadEngine.Api.Security;
using LeadEngine.Application.Interfaces;
using LeadEngine.Infrastructure;
using LeadEngine.Infrastructure.Persistence;
using LeadEngine.Application.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/leadengine-.log", rollingInterval: RollingInterval.Day);
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 64 * 1024;
    options.ValueLengthLimit = 8 * 1024;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 64 * 1024;
});

builder.Services
    .AddControllers(options =>
    {
        options.Filters.Add(new AuthorizeFilter());
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.Configure<AdminAuthOptions>(builder.Configuration.GetSection("AdminAuth"));
builder.Services.AddScoped<AdminAuthService>();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "LeadEngineAdmin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IRequestContext, HttpRequestContext>();
builder.Services.AddScoped<LeadCaptureService>();
builder.Services.AddScoped<LeadConsultaService>();
builder.Services.AddScoped<CampanhaService>();
builder.Services.AddScoped<ICampaignReviewService, CampaignReviewService>();
builder.Services.AddInfrastructure(builder.Configuration);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("lead-capture", limiter =>
    {
        limiter.PermitLimit = 8;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
    options.AddPolicy("public-lead-capture", context =>
    {
        var config = context.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<LeadCaptureOptions>>().Value;
        var key = RequestHashing.HashIp(context.Connection.RemoteIpAddress?.ToString());
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = Math.Max(1, config.MaxLeadsPerIpPerHour),
            Window = TimeSpan.FromHours(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var key = RequestHashing.HashIp(context.Connection.RemoteIpAddress?.ToString());
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
});

var app = builder.Build();

if (app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", false))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<LeadEngineDbContext>();
    await db.Database.MigrateAsync();
}

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("ClientIpHash", RequestHashing.HashIp(httpContext.Connection.RemoteIpAddress?.ToString()));
    };
});

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("DefaultCors");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapGet("/health", async (LeadEngineDbContext db, CancellationToken ct) =>
{
    await db.Database.CanConnectAsync(ct);
    return Results.Ok(new { status = "healthy" });
}).AllowAnonymous();

app.Run();

public partial class Program;
