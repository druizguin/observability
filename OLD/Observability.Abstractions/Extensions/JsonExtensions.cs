using System.Text.Json;

namespace Observability.Abstractions;
        
/// <summary>
/// Provides helpers to convert a <see cref="JsonElement"/> into a flattened dictionary of label names to values.
/// </summary>
/// <remarks>
/// - The resulting dictionary keys are created by concatenating JSON property names with '.' for nested objects
///   and using index notation like <c>arrayProperty[0]</c> for array elements.
/// - Values are stored as <see cref="JsonElement"/> instances boxed as <see cref="object"/>; callers should
///   extract the appropriate typed value via <see cref="JsonElement"/> APIs (for example <c>GetString()</c>, <c>GetInt32()</c>, etc.).
/// - This utility is intended for producing simple flattened tags suitable for telemetry labeling and does not
///   attempt to perform advanced type mapping or null-coalescing semantics.
/// </remarks>
public static class JsonExtensions
{
    /// <summary>
    /// Converts the provided <see cref="JsonElement"/> into a flattened dictionary.
    /// </summary>
    /// <param name="element">The JSON element to flatten. Can be an object, array or primitive.</param>
    /// <param name="prefix">Prefix for keys in dictionary</param>
    /// <returns>
    /// A dictionary whose keys represent the flattened path to each terminal JSON value and whose values are the
    /// corresponding <see cref="JsonElement"/> boxed as <see cref="object"/>. If the input is an object the top-level
    /// property names will be used as keys; if the input is a primitive the returned dictionary may contain an entry
    /// whose key is an empty string (caller should account for this scenario).
    /// </returns>
    public static IDictionary<string, object?> JsonToDictionary(JsonElement element, string prefix = "")
    {
        var result = new Dictionary<string, object?>();
        FlattenElement(element, result, prefix);

        return result;
    }

    /// <summary>
    /// Recursively flattens a <see cref="JsonElement"/> into the supplied dictionary.
    /// </summary>
    /// <param name="element">The current JSON element being processed.</param>
    /// <param name="dict">Dictionary that receives flattened entries.</param>
    /// <param name="prefix">
    /// Current key prefix representing the path to <paramref name="element"/>. For object properties this
    /// is extended using '<c>.</c>' and for array elements using '<c>[index]</c>'.
    /// </param>
    private static void FlattenElement(JsonElement element, Dictionary<string, object?> dict, string prefix)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    string newPrefix = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                    FlattenElement(property.Value, dict, newPrefix);
                }
                break;

            case JsonValueKind.Array:
                int index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    FlattenElement(item, dict, $"{prefix}[{index}]");
                    index++;
                }
                break;

            default:
                dict[prefix] = element;
                break;
        }
    }
}
