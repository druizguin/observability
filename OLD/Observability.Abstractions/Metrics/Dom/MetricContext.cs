namespace Observability.Abstractions;

public class MetricContext : IObservabilityLabels
{
    /// <summary>
    /// Metric name (logical name before any formatting).
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Optional human readable description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional unit for the metric (for example "ms" or "count").
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// The instrument type to use when registering this metric (counter, gauge, histogram, ...).
    /// </summary>
    public MetricInstrumentType Type { get; set; }

    /// <summary>
    /// Labels (tags) attached to the metric. Keys are label names and values are arbitrary objects (converted to strings when required).
    /// </summary>
    public IDictionary<string, object?> Labels { get; set; } = new Dictionary<string, object?>();
}
