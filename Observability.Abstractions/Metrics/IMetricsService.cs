namespace Observability.Abstractions;

public interface IMetricsService
{
    /// <summary>
    /// Registers a numeric value for the provided <see cref="MetricContext"/>.
    /// Implementations interpret the metric type and record the value accordingly (counter, gauge, histogram).
    /// </summary>
    /// <param name="metric">Metric context describing the metric to record.</param>
    /// <param name="value">Value to record.</param>
    void Register(MetricContext metric, double value);
}

