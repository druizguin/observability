namespace Observability.Abstractions;

public enum MetricInstrumentType
{
    /// <summary>
    /// Represents the gauge type of a measurement or instrument.
    /// </summary>
    Gauge = 0,
    /// <summary>
    /// Represents a counter mode for the operation.
    /// </summary>
    Counter = 1,
    /// <summary>
    /// Represents a counter that can be incremented or decremented.
    /// </summary>
    CounterUpDown = 2,
    /// <summary>
    /// Represents a histogram chart type.
    /// </summary>
    Histogram = 3
}

