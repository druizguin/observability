namespace Observability;

using Observability.Abstractions;

/// <summary>
/// The traces name builder implementation.
/// </summary>
public class LabelNameBuilder : NameBuilderBase, ILabelNameBuilder
{
    /// <summary>
    /// Creates a new instance optionally using a prefix applied to generated trace names.
    /// </summary>
    /// <param name="prefix">Optional prefix (e.g. service or domain) to prepend to generated names.</param>
    public LabelNameBuilder(string? prefix = null) : base(prefix)
    {
    }

    /// <summary>
    /// The error message used when name validation fails.
    /// </summary>
    public override string ErrorMessage => "TraceNameBuilder. Invalid trace Name: null or empty";
}
