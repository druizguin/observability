namespace Observability;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Observability.Abstractions;

/// <summary>
/// Typed observability service that exposes a typed <see cref="ILogger{T}"/> along with traces and metrics.
/// </summary>
/// <typeparam name="T">Type used to scope the logger.</typeparam>
public class ObservabilityService<T> : ObservabilityServiceBase, IObservabilityService<T>
{
    /// <summary>
    /// Creates a typed observability service that uses the provided typed logger.
    /// </summary>
    public ObservabilityService(
        ILogger<T> logger, 
        IOptions<IApplicationCard> applicationCard, 
        ITracesService? traces, IMetricsService? metrics) : base(applicationCard, traces, metrics)
    {
        ArgumentNullException.ThrowIfNull(logger);
        Logger = logger;
    }

    /// <summary>
    /// Gets an <see cref="ILogger{T}"/> instance scoped to the specified type.
    /// </summary>
    public virtual ILogger<T> Logger {get; private set; }
}

/// <summary>
/// Non-generic observability service that resolves an <see cref="ILoggerFactory"/> to produce a logger.
/// </summary>
public class ObservabilityService : ObservabilityServiceBase, IObservabilityService
{
    /// <summary>
    /// Gets the logger instance used for logging operations.
    /// </summary>
    public virtual ILogger Logger { get; internal set; }

    /// <summary>
    /// Constructs an observability service using an <see cref="ILoggerFactory"/>.
    /// </summary>
    public ObservabilityService(
        ILoggerFactory loggerFactory, 
        IOptions<IApplicationCard> applicationCard, 
        ITracesService? traces, 
        IMetricsService? metrics) : 
        base(applicationCard, traces, metrics)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory, nameof(loggerFactory));
        Logger = loggerFactory.CreateLogger(ApplicationCard.Key);
    }
}