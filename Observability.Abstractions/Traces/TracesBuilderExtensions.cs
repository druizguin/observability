namespace Observability.Abstractions;

using System.Diagnostics;

/// <summary>
/// Extension helpers for building and registering trace activities using <see cref="ITracesService"/> and <see cref="TraceBuilder"/>.
/// </summary>
public static class TracesBuilderExtensions
{
    /// <summary>
    /// Creates and configures a <see cref="TraceBuilder"/> using the provided service and name.
    /// </summary>
    /// <param name="service">Traces service used to register the builder.</param>
    /// <param name="name">Activity name.</param>
    /// <param name="activityKind">Optional activity kind (default: Internal).</param>
    /// <returns>A configured <see cref="TraceBuilder"/> instance.</returns>
    public static TraceBuilder Configure(this ITracesService service, 
        string name, 
        ActivityKind activityKind = ActivityKind.Internal)
    {
        ArgumentNullException.ThrowIfNull(service, nameof(service));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new TraceBuilder(name)
        {
            Traces = service,
            ActivityKind = activityKind
        };
    }

    /// <summary>
    /// Sets the activity kind on an existing builder.
    /// </summary>
    /// <param name="builder">The trace builder to modify.</param>
    /// <param name="activityKind">New activity kind.</param>
    /// <returns>The same builder instance.</returns>
    public static TraceBuilder AsType(this TraceBuilder builder, ActivityKind activityKind)
    {
        builder.ActivityKind = activityKind;
        return builder;
    }

    /// <summary>
    /// Finalizes the builder and registers the activity, returning the activity process created by the service.
    /// </summary>
    /// <param name="builder">Builder to register.</param>
    /// <returns>The created <see cref="IActivityProcess"/>.</returns>
    public static IActivityProcess Build(this TraceBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        ArgumentNullException.ThrowIfNull(builder.Traces, nameof(builder.Traces));
        return builder.Traces.RegisterActivity(builder);
    }

    /// <summary>
    /// Convenience helper to create and register an activity with a single call.
    /// </summary>
    /// <param name="service">Traces service to use.</param>
    /// <param name="name">Activity name.</param>
    /// <param name="activityKind">Activity kind.</param>
    /// <returns>The created <see cref="IActivityProcess"/>.</returns>
    public static IActivityProcess Activity(this ITracesService service,
        string name,
        ActivityKind activityKind = ActivityKind.Internal)
    {
        ArgumentNullException.ThrowIfNull(service, nameof(service));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var tb = new TraceBuilder(name)
        {
            Traces = service,
            ActivityKind = activityKind
        };

        return service.RegisterActivity(tb);
    }

    /// <summary>
    /// Creates a child activity using the provided process as parent and registers it.
    /// </summary>
    /// <param name="process">Parent process.</param>
    /// <param name="name">Child activity name.</param>
    /// <param name="activityKind">Child activity kind.</param>
    /// <returns>The new child <see cref="IActivityProcess"/>.</returns>
    public static IActivityProcess ChildActivity(this IActivityProcess process,
        string name,
        ActivityKind activityKind = ActivityKind.Internal)
    {
        ArgumentNullException.ThrowIfNull(process, nameof(process));
        ArgumentNullException.ThrowIfNull(process.Service, nameof(process.Service));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var tb = new TraceBuilder(name)
        {
            Traces = process.Service,
            Activity = process.Activity,
            ActivityKind = activityKind
        };

        return process.Service.RegisterActivity(tb);
    }

    /// <summary>
    /// Creates a <see cref="TraceBuilder"/> representing a child activity that can be configured and registered later.
    /// </summary>
    /// <param name="process">Parent process.</param>
    /// <param name="name">Child activity name.</param>
    /// <param name="activityKind">Activity kind.</param>
    /// <returns>A configured <see cref="TraceBuilder"/>.</returns>
    public static TraceBuilder CreateChildActivity(this IActivityProcess process,
        string name,
        ActivityKind activityKind = ActivityKind.Internal)
    {
        ArgumentNullException.ThrowIfNull(process, nameof(process));
        ArgumentNullException.ThrowIfNull(process.Service, nameof(process.Service));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var tb = new TraceBuilder(name)
        {
            Traces = process.Service,
            ActivityKind = activityKind
        };

        return tb;
    }
}