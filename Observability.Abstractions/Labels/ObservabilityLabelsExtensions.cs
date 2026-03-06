namespace Observability.Abstractions;

public static class ObservabilityLabelsExtensions
{
    private static Lazy<ContextLabelBuilder> labelBuilderLazy  = new Lazy<ContextLabelBuilder>(()=>new ContextLabelBuilder());
    private static ContextLabelBuilder LabelBuilder => labelBuilderLazy.Value;

    /// <summary>
    /// Adds or updates a single label on the provided builder/tags container.
    /// </summary>
    public static T Label<T>(this T tags, string key, object? value)
        where T : IObservabilityLabels
    {
        tags.Labels[key] = value;
        return tags;
    }

    /// <summary>
    /// Adds multiple labels supplied as tuples.
    /// </summary>
    public static T Label<T>(this T tags, params (string key, object? value)[] parameters)
        where T : IObservabilityLabels
    {
        tags.Labels.AddRange(parameters);
        return tags;
    }

    /// <summary>
    /// Adds label entries from a dictionary to the builder/tags container.
    /// </summary>
    public static T LabelFromDictionary<T>(this T builder, IDictionary<string, object?> dic)
        where T : IObservabilityLabels
    {
        builder.Labels.AddRange(dic);
        return builder;
    }

    /// <summary>
    /// Adds labels extracted from an arbitrary context object.
    /// </summary>
    /// <typeparam name="T">Type implementing <see cref="IObservabilityLabels"/>.</typeparam>
    /// <typeparam name="TContext">Context object type used to generate labels.</typeparam>
    /// <param name="tags">Target labels container.</param>
    /// <param name="context">Context object to extract labels from.</param>
    /// <param name="prefix">Optional prefix applied to each generated label key.</param>
    /// <returns>The same tags instance for fluent calls.</returns>
    public static T LabelContext<T, TContext>(this T tags, TContext context, string? prefix = null)
        where T : IObservabilityLabels
        where TContext : class
    {
        prefix = prefix != null ? prefix.TrimEnd('.') + "." : "";

        try
        {
            var labels = LabelBuilder.LabelContext(context, prefix);
            tags.Labels.AddRange(labels);
        }
        catch (Exception ex)
        {
            throw new InvalidCastException("No se pudo serializar el contexto: " + ex.Message);
        }

        return tags;
    }
}

