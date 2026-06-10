namespace Observability;

using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using System.Diagnostics;

using Observability.Abstractions;

/// <summary>
/// Implements <see cref="ITracesService"/> using an <see cref="ActivitySource"/>.
/// Responsible for starting activities and producing <see cref="IActivityProcess"/> instances.
/// </summary>
public class TracesService : ITracesService
{
    private readonly ActivitySource _activitySource;
    private readonly IServiceScopeFactory _serviceProvider;

    /// <summary>
    /// Creates a new instance of <see cref="TracesService"/>.
    /// </summary>
    /// <param name="activitySource">ActivitySource used to start activities.</param>
    /// <param name="scopeFactory">Scope factory used to create <see cref="ActivityProcess"/> instances from DI.</param>
    public TracesService(ActivitySource activitySource, IServiceScopeFactory scopeFactory)
    {
        _activitySource = activitySource;
        _serviceProvider = scopeFactory;
    }

    /// <summary>
    /// Creates or returns the current <see cref="Activity"/> corresponding to the provided <see cref="TraceBuilder"/>
    /// .</summary>
    /// <param name="builder">Trace builder that configures the activity.</param>
    /// <returns>An <see cref="Activity"/> instance or <c>null</c> if no activity was started.</returns>
    public Activity? GetCurrentActivity(TraceBuilder builder)
    {
        return NewActivity(builder);
    }

    /// <summary>
    /// Registers a new activity and returns an <see cref="IActivityProcess"/> that manages its lifecycle.
    /// </summary>
    /// <param name="builder">Trace builder that configures the activity.</param>
    /// <returns>An <see cref="IActivityProcess"/> that wraps the created activity.</returns>
    public IActivityProcess RegisterActivity(TraceBuilder builder)
    {
        Activity? activity = NewActivity(builder);

        ActivityProcess process;
        using (var scope = _serviceProvider.CreateScope())
        {
            process = scope.ServiceProvider.GetService<ActivityProcess>()
              ?? throw new InvalidOperationException("No se pueden crear ActivityProcess. El servicio no está registrado");
        }

        process.Activity = activity;
        process.Service = this;

        return process;
    }

    private static PropagationContext? PropagationContextFactory(TraceBuilder builder)
    {
        if (builder.PropagationContext != null)
        {
            var parentContext = builder.PropagationContext.Value;
            Baggage.Current = parentContext.Baggage;
            return parentContext;
        }

        return null;
    }

    /// <summary>
    /// Internal activity creation logic used by the public methods.
    /// Supports using propagation context, explicit parent activity, or current activity.
    /// </summary>
    /// <param name="builder">Trace builder with configuration.</param>
    /// <returns>Created <see cref="Activity"/> or <c>null</c>.</returns>
    internal Activity? NewActivity(TraceBuilder builder)
    {
        Activity? activity = Activity.Current;

        var parentContext = PropagationContextFactory(builder);

        if (parentContext.HasValue)
        {
            activity = _activitySource.StartActivity(
                    builder.Name,
                    kind: builder.ActivityKind,
                    startTime: DateTimeOffset.UtcNow,
                    parentContext: parentContext.Value.ActivityContext);

            return activity;
        }

        if (activity != null && builder.Activity is null) return activity;

        Activity? parent = builder.Activity != null
        ? builder.Activity
        : Activity.Current;

        activity = _activitySource.StartActivity(
                builder.Name,
                kind: builder.ActivityKind,
                parentId: parent?.Id,
                startTime: DateTimeOffset.UtcNow);

        if (builder.Activity != null)
        {
            builder.Activity?.AddEvent(new ActivityEvent(builder.Name, DateTimeOffset.UtcNow));
        }

        return activity;
    }
}
