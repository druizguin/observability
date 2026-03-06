    namespace Observability;

using System.Diagnostics.Metrics;
using Observability.Abstractions;

/// <summary>
/// Concrete implementation of <see cref="IMetricsService"/> backed by a <see cref="Meter"/>.
/// Adapts <see cref="MetricContext"/> instances to the System.Diagnostics.Metrics API.
/// </summary>
public class MetricsService : IMetricsService
{
    private Meter _meter;
    private readonly IMetricNameBuilder metricNameBuilder;
    private readonly ILabelNameBuilder _labelNameBuilder;


    /// <summary>
    /// Initializes a new instance of <see cref="MetricsService"/>.
    /// </summary>
    /// <param name="metricNameBuilder">Name builder used to normalize metric names.</param>
    /// <param name="labelNameBuilder">Name builder used to normalize tags names.</param>
    /// <param name="meter">Meter used to create instruments and record measurements.</param>
    public MetricsService(
        IMetricNameBuilder metricNameBuilder,
        ILabelNameBuilder labelNameBuilder,
        Meter meter)
    {
        ArgumentNullException.ThrowIfNull(meter, nameof(meter));
        ArgumentNullException.ThrowIfNull(metricNameBuilder, nameof(metricNameBuilder));
        _meter = meter;
        this.metricNameBuilder = metricNameBuilder;
        _labelNameBuilder = labelNameBuilder;
    }

    /// <summary>
    /// Registers a numeric value for the provided <see cref="MetricContext"/>.
    /// The method maps the <see cref="MetricInstrumentType"/> to the corresponding Meter instrument.
    /// </summary>
    /// <param name="metric">Context that describes the metric to record.</param>
    /// <param name="value">Numeric value to record.</param>
    public void Register(MetricContext metric, double value)
    {
        string metertype = MetricTypeNameConverter(metric.Type);
        var metricName = metricNameBuilder.NormalizeName(metric.Name, metertype);

        KeyValuePair<string, object?>[] labels = metric.Labels
          .Where(p => p.Value != null)
          .Select(p => new KeyValuePair<string, object?>
                (_labelNameBuilder.NormalizeName(p.Key), p.Value!))
          .ToArray();

        switch (metric.Type)
        {
            case MetricInstrumentType.Counter:
                var counter = _meter.CreateCounter<double>(metricName,
                    unit: metric.Unit,
                    description: metric.Description);
                counter.Add(value, labels);
                break;

            case MetricInstrumentType.CounterUpDown:
                var updown = _meter.CreateUpDownCounter<double>(metricName,
                    unit: metric.Unit,
                    description: metric.Description);
                updown.Add(value, labels);
                break;

            case MetricInstrumentType.Gauge:
                var gauge = _meter.CreateGauge<double>(metricName,
                       unit: metric.Unit,
                       description: metric.Description);
                gauge.Record(value, labels);
                break;

            case MetricInstrumentType.Histogram:
                var histogram = _meter.CreateHistogram<double>(metricName);
                histogram.Record(value, labels);
                break;
        }
    }

    private string MetricTypeNameConverter(MetricInstrumentType type)
    {
        switch (type)
        {
            case MetricInstrumentType.Counter:
            case MetricInstrumentType.CounterUpDown: return "count";
            case MetricInstrumentType.Gauge: return "gauge";
            default:/* MetricInstrumentType.Histogram: */ return "hist";
        }
    }
}