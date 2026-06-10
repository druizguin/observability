namespace Observability.Abstractions;

using OpenTelemetry.Context.Propagation;

/// <summary>
/// Builder extensions include propagation context information when required.
/// </summary>
public class TraceBuilder : TraceBuilderBase
{
    /// <summary>
    /// Constructs a trace builder with the specified trace name.
    /// </summary>
    /// <param name="name"></param>
    public TraceBuilder(string name) : base(name)
    {
    }

    /// <summary>
    /// Optional OpenTelemetry propagation context to use as parent when starting the activity.
    /// </summary>
    public PropagationContext? PropagationContext { get; set; }
}
