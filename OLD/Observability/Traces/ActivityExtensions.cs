namespace Observability;

using System.Diagnostics;
using Observability.Abstractions;

/// <summary>
/// Provides extension methods for the <see cref="Activity"/> class to enhance its functionality.
/// </summary>

internal static class ActivityExtensions
{
    /// <summary>
    /// Sets tags on the specified <see cref="Activity"/> using key-value pairs from the provided dictionary.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/> on which to set the tags. If null, no operation is performed.</param>
    /// <param name="tags">A dictionary containing the tags to set, where each key is a tag name and each value is the tag value. Cannot be
    /// null.</param>
    /// <param name="nameBuilder">An instance of <see cref="ILabelNameBuilder"/> used to generate metric names for the tags.</param>
    /// <returns>The <see cref="Activity"/> with the tags set, or null if the input activity was null.</returns>
    /// <remarks>Only non-null values from the <paramref name="tags"/> dictionary are set as tags on the <paramref
    /// name="activity"/>. The <paramref name="nameBuilder"/> is used to transform the dictionary keys before setting them
    /// as tags.</remarks>
    internal static Activity? SetTagsFromDictionary(
        this Activity? activity, 
        IDictionary<string, object?> tags, 
        ILabelNameBuilder nameBuilder)
    {
        if (activity == null) return activity;

        ArgumentNullException.ThrowIfNull(tags, nameof(tags));

        foreach (var item in tags.Where(p => p.Value != null))
        {
            var key = nameBuilder.NormalizeName(item.Key);
            activity?.SetTag(item.Key, item.Value?.ToString());
        }

        return activity;
    }
}
