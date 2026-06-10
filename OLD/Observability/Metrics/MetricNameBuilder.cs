using Observability.Abstractions;

namespace Observability;

/// <summary>
/// Simple metric name normalizer that composes segments and an optional prefix into a dot-separated metric name.
/// Replaces whitespace and underscores with dot separators and lower-cases the result.
/// </summary>
public class MetricNameBuilder : NameBuilderBase, IMetricNameBuilder
{
    /// <summary>
    /// Creates a new instance optionally using a prefix applied to generated metric names.
    /// </summary>
    /// <param name="prefix">Optional prefix (e.g. service or domain) to prepend to generated names.</param>
    public MetricNameBuilder(string? prefix = null) : base(prefix)
    {
    }

    /// <summary>
    /// The error message used when name validation fails.
    /// </summary>
    public override string ErrorMessage => "MetricNameBuilder. Invalid metric Name: null or empty";
}
