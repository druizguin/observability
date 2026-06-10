namespace Observability; 

using OpenTelemetry.Metrics;

/// <summary>
/// Opciones de configuración para la instrumentación de métricas de la aplicación.
/// Diseñada para integrarse con OpenTelemetry (<see cref="MeterProviderBuilder"/>)
/// y facilitar la selección de <c>meters</c>, prefijos y exportadores.
/// </summary>
public class MetricsOptions
{
    /// <summary>
    /// Prefijo que se antepone al nombre de cada métrica publicada.
    /// Útil para agrupar o evitar colisiones de nombres entre servicios.
    /// Por defecto es cadena vacía.
    /// </summary>
    public string Prefix { get; set; } = "";


    /// <summary>
    /// Indica si se incluye el nombre del servicio en las métricas (por ejemplo,
    /// como parte del nombre o de etiquetas/atributos estándar).
    /// Por defecto es <c>true</c>.
    /// </summary>
    public bool IncludeServiceName { get; set; } = true;

    /// <summary>
    /// Conjunto de nombres de <c>Meter</c> (instrumentaciones) a habilitar.
    /// Solo los meters listados serán registrados en el proveedor de métricas.
    /// Por defecto, una matriz vacía.
    /// </summary>

    public string[] Meters { get; set; } = Array.Empty<string>();


    /// <summary>
    /// Cadena opcional con cabeceras para exportadores remotos.
    /// El formato depende del exportador (por ejemplo, pares key=value separados por comas).
    /// Puede ser <c>null</c> si no aplica.
    /// </summary>
    public string? Headers { get; set; }


    /// <summary>
    /// Si es <c>true</c>, habilita un exportador de consola para métricas.
    /// Útil para depuración local o entornos de desarrollo.
    /// </summary>
    public bool WithConsoleExporter { get; set; }

    /// <summary>
    /// intervalo en milisegundos entre exportaciones de métricas.
    /// </summary>
    public int ExportIntervalMilliseconds { get; set; } = 1000;

    /// <summary>
    /// Acción interna para extender la construcción del <see cref="MeterProviderBuilder"/>.
    /// Permite que el host añada instrumentaciones, vistas o exportadores personalizados.
    /// </summary>
    internal Action<MeterProviderBuilder>? MeterBuilderAction { get;  set; }
}
