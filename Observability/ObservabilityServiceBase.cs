namespace Observability;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Observability.Abstractions;

/// <summary>
/// Base implementation that provides common observability members: Logs (abstract), Traces, Metrics and ApplicationCard.
/// </summary>
public abstract class ObservabilityServiceBase : IObservabilityMetrics, IObservabilityTraces, IAplicationIdentity
{
    /// <summary>
    /// Provides access to traces service <see cref="ITracesService"/> to traces purpose.
    /// </summary>
    public virtual ITracesService Traces { get; }


    /// <summary>
    /// Provides access to metrics service <see cref="IMetricsService"/> to metrics purpose.
    /// </summary>
    public virtual IMetricsService Metrics { get; }

    /// <summary>
    /// Provides the application card identity for an application <see cref="IApplicationCard"/>.
    /// </summary>
    public IApplicationCard ApplicationCard => _appCard;

    private readonly IApplicationCard _appCard;

    /// <summary>
    /// Initializes the base service with required dependencies.
    /// </summary>
    /// <param name="applicationCard">Application card options.</param>
    /// <param name="traces">Traces service.</param>
    /// <param name="metrics">Metrics service.</param>
    public ObservabilityServiceBase(
        IOptions<IApplicationCard> applicationCard,
        ITracesService? traces,
        IMetricsService? metrics)
    {
        ArgumentNullException.ThrowIfNull(traces, nameof(traces));
        ArgumentNullException.ThrowIfNull(metrics, nameof(metrics));
        ArgumentNullException.ThrowIfNull(applicationCard, nameof(applicationCard));

        _appCard = applicationCard.Value;
        Traces = traces;
        Metrics = metrics;
    }
}
