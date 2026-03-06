namespace Observability.Abstractions;

public static class MetricsServiceExtensions
{
    /// <summary>
    /// Sets the human-readable description for the metric builder.
    /// </summary>
    /// <typeparam name="TMeter">A type derived from <see cref="MetricBuilder"/>.</typeparam>
    /// <param name="builder">The metric builder instance to configure.</param>
    /// <param name="description">The description to assign to the metric.</param>
    /// <returns>The same builder instance allowing fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is <c>null</c>.</exception>
    public static TMeter WithDescription<TMeter>(this TMeter builder, string description) where TMeter : MetricBuilder
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        builder.Description = description;
        return builder;
    }

    /// <summary>
    /// Sets the unit for the metric builder (for example "ms", "count", etc.).
    /// </summary>
    /// <typeparam name="TMeter">A type derived from <see cref="MetricBuilder"/>.</typeparam>
    /// <param name="builder">The metric builder instance to configure.</param>
    /// <param name="unit">The unit string to assign to the metric.</param>
    /// <returns>The same builder instance allowing fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is <c>null</c>.</exception>
    public static TMeter WithUnit<TMeter>(this TMeter builder, string unit) where TMeter : MetricBuilder
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        builder.Unit = unit;
        return builder;
    }
}
