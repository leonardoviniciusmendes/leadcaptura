using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LeadEngine.Infrastructure.GoogleAds;

public sealed class GoogleAdsMetricsSyncWorker(IServiceProvider serviceProvider) : BackgroundService
{
    private readonly SemaphoreSlim gate = new(1, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = serviceProvider.CreateScope();
            var resolver = scope.ServiceProvider.GetRequiredService<IConfigurationResolver>();
            var enabled = bool.TryParse((await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "MetricsSyncEnabled", stoppingToken)).Value, out var value) && value;
            var interval = int.TryParse((await resolver.ResolveAsync(CategoriaConfiguracao.GoogleAds, "MetricsSyncIntervalMinutes", stoppingToken)).Value, out var configuredInterval) ? configuredInterval : 1440;
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
                catch
                {
                    // O historico de sincronizacao registra falhas por publicacao; o worker nao deve derrubar a aplicacao.
                }
                finally
                {
                    gate.Release();
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(Math.Clamp(interval, 5, 10080)), stoppingToken);
        }
    }
}
