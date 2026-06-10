using Observability.Abstractions;

namespace Observability;

/// <summary>
/// Builder base for metric and trace names.
/// </summary>
public abstract class NameBuilderBase : IMetricNameBuilder
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="prefix"></param>
    protected NameBuilderBase(string? prefix = null)
    {
        Format = !string.IsNullOrWhiteSpace(prefix) ? $"{prefix}.{{0}}" : $"{{0}}";
    }

    /// <summary>
    /// The format string used to compose metric names.
    /// </summary>
    public virtual string Format { get; }

    /// <summary>
    /// The error message used when name validation fails.
    /// </summary>
    public abstract string ErrorMessage { get; }

    /// <summary>
    /// Builds a normalized metric name from one or more segments.
    /// </summary>
    /// <param name="names">Name segments used to compose the metric name.</param>
    /// <returns>Normalized metric name.</returns>
    public virtual string NormalizeName(params string[] names)
    {
        if (names == null || names.Length == 0)
            throw new ArgumentNullException(ErrorMessage, nameof(names));

        names = names.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();

        var normName = BuildMetricName(names);
        return normName;
    }

    /// <summary>
    /// Builds the metric name.
    /// </summary>
    /// <param name="names">set of names to compose.</param>
    /// <returns>The metric name.</returns>
    public virtual string BuildMetricName(params string[] names)
    {
        var pathName = string.Join(".", names)
           .Replace(" ", "_")
           .Replace("_", ".");

        return string.Format(Format, pathName).ToLowerInvariant();
    }
}
