namespace Observability.Abstractions;

public static class DictionaryExtensions
{
    /// <summary>
    /// Converts the dictionary to an array of key/value pairs suitable for telemetry tag APIs.
    /// </summary>
    /// <param name="dic">Source dictionary whose entries will be converted to tags.</param>
    /// <returns>
    /// An array of <see cref="KeyValuePair{String,Object}"/> containing the same keys and values as the source dictionary.
    /// If the source contains zero elements an empty array is returned.
    /// </returns>
    /// <remarks>
    /// The method does not mutate the source dictionary. If <paramref name="dic"/> is <c>null</c> a <c>NullReferenceException</c> will occur.
    /// </remarks>
    public static KeyValuePair<string, object?>[] ToTags(this IDictionary<string, object?> dic)
    {
        return dic
            .Select(x => new KeyValuePair<string, object?>(x.Key, x.Value))
            .ToArray();
    }

    /// <summary>
    /// Adds or updates the entries from <paramref name="add"/> into <paramref name="dic"/>.
    /// Existing keys are overwritten with the values from <paramref name="add"/>.
    /// </summary>
    /// <typeparam name="TK">Type of the dictionary keys.</typeparam>
    /// <typeparam name="TV">Type of the dictionary values.</typeparam>
    /// <param name="dic">Target dictionary to update.</param>
    /// <param name="add">Dictionary containing entries to add or update.</param>
    /// <remarks>
    /// Both <paramref name="dic"/> and <paramref name="add"/> must not be <c>null</c>.
    /// The operation assigns values using the indexer so any existing value for a matching key is replaced.
    /// </remarks>
    public static void AddRange<TK, TV>(this IDictionary<TK, TV> dic, IDictionary<TK, TV> add)
    {
        foreach (var item in add) dic[item.Key] = item.Value;
    }

    /// <summary>
    /// Adds or updates the specified <paramref name="items"/> into <paramref name="dic"/>.
    /// Existing keys are overwritten.
    /// </summary>
    /// <typeparam name="TK">Type of the dictionary keys.</typeparam>
    /// <typeparam name="TV">Type of the dictionary values.</typeparam>
    /// <param name="dic">Target dictionary to update.</param>
    /// <param name="items">Key/value pairs to add or update.</param>
    public static void AddRange<TK, TV>(this IDictionary<TK, TV> dic, params KeyValuePair<TK, TV>[] items)
    {
        foreach (var item in items) dic[item.Key] = item.Value;
    }

    /// <summary>
    /// Adds or updates the specified <paramref name="items"/> into <paramref name="dic"/>.
    /// This overload accepts tuples for simpler inline usage.
    /// </summary>
    /// <typeparam name="TK">Type of the dictionary keys.</typeparam>
    /// <typeparam name="TV">Type of the dictionary values.</typeparam>
    /// <param name="dic">Target dictionary to update.</param>
    /// <param name="items">Tuples of key and value to add or update.</param>
    public static void AddRange<TK, TV>(this IDictionary<TK, TV> dic, params (TK key, TV value)[] items)
    {
        foreach (var item in items)
        {
            dic[item.key] = item.value;
        }
    }
}

