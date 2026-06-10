namespace Rating.BusinessLayer.HostedServices;
using Microsoft.Extensions.Hosting;
using Unir.Framework.Observability.Abstractions;

public class ObservabilityMetricsService : BackgroundService
{
    private readonly IMetricsService _metricsService;

    public ObservabilityMetricsService(IMetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    // ...

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string producto ="Producto A";

        // Uso de métrica de tipo contador
        _metricsService
            .Counter("contador.ejemplo", "Descripción del contador")
            .Label("producto", producto)
            .Up(1);

        return Task.CompletedTask;
    }
}
