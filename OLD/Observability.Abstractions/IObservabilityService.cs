using Microsoft.Extensions.Logging;

namespace Observability.Abstractions;

/// <summary>
/// Exposes the surface for an observability service: logging, metrics, traces and application metadata.
/// Implementations provide access to the <see cref="ILogger"/>, <see cref="IMetricsService"/>, <see cref="ITracesService"/> and <see cref="IApplicationCard"/>.
/// </summary>
public interface IObservabilityService : IObservabilityMetrics, IObservabilityTraces, IAplicationIdentity
{
    /// <summary>
    /// Gets an <see cref="ILogger"/> instance scoped to the current application or component.
    /// </summary>
    ILogger Logger { get; }
}

/// <summary>
/// Interface exposing metrics capabilities.
/// </summary>
public interface IObservabilityMetrics
{
    /// <summary>
    /// Gets the metrics service used to record metrics.
    /// </summary>
    IMetricsService Metrics { get; }
}

/// <summary>
/// Interface exposing traces capabilities.
/// </summary>
public interface IObservabilityTraces
{

    /// <summary>
    /// Gets the traces service used to start and register activities (traces).
    /// </summary>
    ITracesService Traces { get; }
}

/// <summary>
/// Interface exposing application identity capabilities.
/// </summary>
public interface IAplicationIdentity
{

    /// <summary>
    /// Gets the application card that describes the running application (key, version, environment).
    /// </summary>
    IApplicationCard ApplicationCard { get; }
}

/// <summary>
/// Exposes the surface for an observability service: logging, metrics, traces and application metadata.
/// Implementations provide access to the 
/// <see cref="ILogger" /> 
/// <see cref="IMetricsService"/>,
/// <see cref="ITracesService"/> and <see cref="IApplicationCard"/>.
/// </summary>
/// <typeparam name="T">The type whose name is used for the logger category name.</typeparam>
public interface IObservabilityService<out T> : IAplicationIdentity, IObservabilityMetrics, IObservabilityTraces
{
    /// <summary>
    /// Gets an <see cref="ILogger{T}"/> instance scoped to the specified type.
    /// </summary>
    ILogger<T> Logger { get; }
}