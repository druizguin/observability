namespace Observability.Abstractions;

public static class MetricsServiceBuilderExtensions
{
    /// <summary>
    /// Creates a counter metric builder for the specified metric name.
    /// </summary>
    /// <param name="service">Metrics service used to register the metric.</param>
    /// <param name="name">Logical metric name (will be normalized by the implementation).</param>
    /// <param name="description">Optional human readable description for the metric.</param>
    /// <param name="unit">Optional unit for the metric (for example "ms" or "count").</param>
    /// <param name="tags">Optional initial labels to attach to the metric.</param>
    /// <returns>A configured <see cref="CounterMetricBuilder"/> instance.</returns>
    public static CounterMetricBuilder Counter(this IMetricsService service,
        string name, string? description = null, string? unit = null, IDictionary<string, object?>? tags = null)
    {
        return new CounterMetricBuilder()
            .Configure(service, name, MetricInstrumentType.Counter,
                description: description,
                unit: unit,
                tags: tags);
    }

    /// <summary>
    /// Creates an up/down counter metric builder for the specified metric name.
    /// </summary>
    /// <param name="service">Metrics service used to register the metric.</param>
    /// <param name="name">Logical metric name.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="unit">Optional unit.</param>
    /// <param name="tags">Optional initial labels.</param>
    /// <returns>A configured <see cref="CounterUpDownMetricBuilder"/> instance.</returns>
    public static CounterUpDownMetricBuilder UpDownCounter(this IMetricsService service,
        string name, string? description = null, string? unit = null, IDictionary<string,object?>? tags = null)
    {
        return new CounterUpDownMetricBuilder()
           .Configure(service, name, MetricInstrumentType.CounterUpDown,
               description: description,
               unit: unit,
               tags: tags);
    }

    /// <summary>
    /// Creates a gauge metric builder for the specified metric name.
    /// </summary>
    /// <param name="service">Metrics service used to register the metric.</param>
    /// <param name="name">Logical metric name.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="unit">Optional unit.</param>
    /// <param name="tags">Optional initial labels.</param>
    /// <returns>A configured <see cref="GaugeMetricBuilder"/> instance.</returns>
    public static GaugeMetricBuilder Gauge(this IMetricsService service,
        string name, string? description = null, string? unit = null, IDictionary<string, object?>? tags = null)
    {
        return new GaugeMetricBuilder()
         .Configure(service, name, MetricInstrumentType.Gauge,
             description: description,
             unit: unit,
               tags: tags);
    }

    /// <summary>
    /// Creates a histogram metric builder for the specified metric name.
    /// </summary>
    /// <param name="service">Metrics service used to register the metric.</param>
    /// <param name="name">Logical metric name.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="unit">Optional unit.</param>
    /// <param name="tags">Optional initial labels.</param>
    /// <returns>A configured <see cref="HistogramMetricBuilder"/> instance.</returns>
    public static HistogramMetricBuilder Histogram(this IMetricsService service,
        string name, string? description = null, string? unit = null, IDictionary<string, object?>? tags = null)
    {
        return new HistogramMetricBuilder()
         .Configure(service, name, MetricInstrumentType.Histogram,
             description: description,
             unit: unit,
             tags: tags);
    }

    /// <summary>
    /// Internal helper that configures a <see cref="MetricBuilder"/>-derived instance with the provided values.
    /// </summary>
    /// <typeparam name="T">A type derived from <see cref="MetricBuilder"/>.</typeparam>
    /// <param name="builder">Builder instance to configure.</param>
    /// <param name="service">Metrics service used to register values.</param>
    /// <param name="name">Metric name.</param>
    /// <param name="type">Instrument type.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="unit">Optional unit.</param>
    /// <param name="tags">Optional initial labels dictionary.</param>
    /// <returns>The same builder instance for fluent usage.</returns>
    private static T Configure<T>(this T builder, IMetricsService service,
        string name,
        MetricInstrumentType type,
        string? description = null,
        string? unit = null,
        IDictionary<string, object?>? tags = null) where T : MetricBuilder
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        builder.Service = service;
        builder.Name = name;
        builder.Type = type;
        builder.Description = description;
        builder.Unit = unit;

        if (tags != null) builder.Labels = tags;

        return builder;
    }

    /// <summary>
    /// Records a value for a configured gauge metric.
    /// </summary>
    /// <param name="builder">Configured gauge builder.</param>
    /// <param name="value">Value to record (default is 1).</param>
    public static void Record(this GaugeMetricBuilder builder, double value = 1)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(builder.Service);

        builder.Service.Register(builder, value);
    }

    /// <summary>
    /// Increments the configured counter by the specified value (default is 1).
    /// </summary>
    /// <param name="builder">Configured counter builder.</param>
    /// <param name="value">Amount to increment.</param>
    public static void Up(this CounterMetricBuilder builder, double value = 1)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(builder.Service);

        builder.Service.Register(builder, value);
    }

    /// <summary>
    /// Records a value for a configured up/down counter metric.
    /// </summary>
    /// <param name="builder">Configured up/down counter builder.</param>
    /// <param name="value">Value to record (can be positive or negative).</param>
    public static void Record(this CounterUpDownMetricBuilder builder, double value = 1)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(builder.Service);

        builder.Service.Register(builder, value);
    }

    /// <summary>
    /// Records a value for a configured histogram metric.
    /// </summary>
    /// <param name="builder">Configured histogram builder.</param>
    /// <param name="value">Value to record (default is 1).</param>
    public static void Record(this HistogramMetricBuilder builder, double value = 1)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(builder.Service);

        builder.Service.Register(builder, value);
    }
}

