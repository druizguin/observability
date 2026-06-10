namespace Rating.BusinessLayer.HostedServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Unir.Framework.Observability.Abstractions;

public class  ObservabilityDemoService : BackgroundService
{
    private readonly IObservabilityService<ObservabilityDemoService> _observabilityService;

    public ObservabilityDemoService(IObservabilityService<ObservabilityDemoService> observabilityService)
    {
        _observabilityService = observabilityService;
    }

    // ...

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Uso de trazas
        _observabilityService.Traces
            .Activity("Demo Activity", ActivityKind.Internal)
            .Execute(activity =>
            {
                //Escritura de logs
                _observabilityService.Logger.LogInformation("Starting Observability Demo Service");

                // [Lógica de la actividad]

                //Creación de métricas
                string producto = "Producto A";
                _observabilityService.Metrics
                    .Counter("contador.ejemplo", "Descripción del contador")
                    .Label("producto", producto)
                    .Up(1);

                _observabilityService
                    .Metrics
                    .Counter("contador.ejemplo")
                    .WithDescription("Descripción actualizada del contador")
                    .WithUnit("count")
                    .Up();

                _observabilityService
                    .Metrics
                    .UpDownCounter("contador.ejemplo")
                    .WithDescription("Descripción actualizada del contador")
                    .WithUnit("count")
                    .Record(-1);

                //Etiquetas
                var counter = _observabilityService.Metrics
                    .Counter("contador.ejemplo.etiquetas")
                    .Labels["etiqueta 1"] = "valor 1"
                    ; // Modo diccionario

                _observabilityService.Metrics
                    .Counter("contador.ejemplo.etiquetas")
                    .Label("etiqueta 1", "valor 1"); // Método unitario

                _observabilityService.Metrics
                    .Counter("contador.ejemplo.etiquetas")
                    .Label(("etiqueta 1", "valor 1"), ("etiqueta 2", 2)); // pares clave/valor



            });

        return Task.CompletedTask;
    }
}
