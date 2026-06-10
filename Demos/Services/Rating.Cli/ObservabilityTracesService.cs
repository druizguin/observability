namespace Rating.BusinessLayer.HostedServices;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using Unir.Framework.Observability.Abstractions;

public class ObservabilityTracesService : BackgroundService
{
    private readonly ITracesService _tracesService;

    public ObservabilityTracesService(ITracesService tracesService)
    {
        _tracesService = tracesService;
    }

    // ...

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Uso
        _tracesService
            .Activity("Demo Activity", ActivityKind.Internal)
            .Execute(activity =>
            {
                // Lógica de la actividad

            });

        return Task.CompletedTask;
    }
}
