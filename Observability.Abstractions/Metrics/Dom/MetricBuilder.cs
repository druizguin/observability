namespace Observability.Abstractions;

public class MetricBuilder : MetricContext
{
    /// <summary>
    /// Internal reference to the metrics service that will be used to register the metric. Not intended for public consumption.
    /// </summary>
    internal IMetricsService? Service { get; set; }
}

