namespace Rating.BusinessLayer.HostedServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Unir.Framework.Observability.Abstractions;

public class ObservabilityDemoLogService : BackgroundService
{
    private readonly IObservabilityService _observabilityService;
    private readonly ILogger<ObservabilityDemoLogService> _logger;

    public ObservabilityDemoLogService(IObservabilityService observabilityService, ILogger<ObservabilityDemoLogService> logger)
    {
        _observabilityService = observabilityService;
        _logger = logger;
    }

    // ...

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Uso de trazas
        _observabilityService.Traces
            .Activity("Demo Activity", ActivityKind.Internal)
            .Execute(activity =>
            {
                //Escritura de logs fuera de observability
                _logger.LogInformation("Starting Observability Demo Service");

                // [Lógica de la actividad]

                //Creación de métricas
                string producto = "Producto A";
                _observabilityService.Metrics
                    .Counter("contador.ejemplo", "Descripción del contador")
                    .Label("producto", producto)
                    .Up(1);
            });

        return Task.CompletedTask;
    }
}
