namespace Observability.Abstractions;

public interface IObservabilityLabels
{
    /// <summary>
    /// Labels dictionary. Keys are label names and values are label values (nullable).
    /// </summary>
    IDictionary<string, object?> Labels { get; }
}

