namespace Observability.Abstractions
{
    public interface ILabelNameBuilder
    {
        /// <summary>
        /// Constructs a tag name by concatenating the provided name segments.
        /// </summary>
        /// <param name="names">An array of strings representing the segments of the metric name. Each segment is concatenated with a
        /// separator to form the full metric name.</param>
        /// <returns>A string representing the full metric name constructed from the provided segments. Returns an empty string
        /// if no segments are provided.</returns>
        string NormalizeName(params string[] names);
    }
}
