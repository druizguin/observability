namespace Observability.Abstractions;

public interface IMetricNameBuilder
{
    /// <summary>
    /// Returns a normalized metric name from the provided name parts.
    /// </summary>
    /// <param name="names">Segments to compose the metric name.</param>
    /// <returns>Normalized metric name string.</returns>
    string NormalizeName(params string[] names);
}
