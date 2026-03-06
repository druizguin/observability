namespace Observability.Abstractions;

using System.Diagnostics;

/// <summary>
/// Base builder used to configure an activity (trace) before creating or registering it.
/// </summary>
public class TraceBuilderBase
{
    /// <summary>
    /// Creates a new <see cref="TraceBuilderBase"/> instance with the provided activity name.
    /// </summary>
    /// <param name="name"></param>
    public TraceBuilderBase(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }
    /// <summary>
    /// Optional existing activity to use as parent or reference. Can be <c>null</c>.
    /// </summary>
    public Activity? Activity { get; set; }

    /// <summary>
    /// The activity kind. Defaults to <see cref="ActivityKind.Internal"/>.
    /// </summary>
    public ActivityKind ActivityKind { get; set; } = ActivityKind.Internal;

    /// <summary>
    /// Internal reference to the traces service. Set by extension methods when constructing the builder.
    /// </summary>
    internal ITracesService? Traces { get; set; }

    /// <summary>
    /// The activity name to start or register.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Free-form context dictionary that consumers can populate and that may be used to create labels/tags.
    /// </summary>
    public Dictionary<string, object?> Context { get; private set; } = new();

    /// <summary>
    /// Returns the configured name. Equivalent to <see cref="Name"/>.
    /// </summary>
    public override string ToString() => Name;
}
