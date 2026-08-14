using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LeadEngine.Infrastructure.GoogleAds;

public sealed class GoogleAdsMetricsSyncWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<GoogleAdsMetricsSyncWorker> logger) : BackgroundService
{
    private readonly SemaphoreSlim gate = new(1, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var interval = 1440;
            try
            {
                using var scope = scopeFactory.CreateScope();
                var resolver = scope.ServiceProvider.GetRequiredService<IConfigurationResolver>();
                var enabled = bool.TryParse((await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "MetricsSyncEnabled", stoppingToken)).Value, out var value) && value;
                interval = int.TryParse((await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "MetricsSyncIntervalMinutes", stoppingToken)).Value, out var configuredInterval) ? configuredInterval : 1440;
                if (enabled && await gate.WaitAsync(0, stoppingToken))
                {
                    try
                    {
                        var days = int.TryParse((await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "MetricsSyncDays", stoppingToken)).Value, out var configuredDays) ? Math.Clamp(configuredDays, 1, 90) : 7;
                        var final = DateOnly.FromDateTime(DateTime.UtcNow);
                        var initial = final.AddDays(-(days - 1));
                        var service = scope.ServiceProvider.GetRequiredService<IGoogleAdsMetricsService>();
                        await service.SincronizarTodasAsync(new GoogleAdsPeriodoRequest(initial, final), stoppingToken);
                    }
                    finally
                    {
                        gate.Release();
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException ex) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogDebug(ex, "Google Ads metrics worker stopped while services were being disposed.");
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Google Ads metrics worker iteration failed.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(Math.Clamp(interval, 5, 10080)), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
