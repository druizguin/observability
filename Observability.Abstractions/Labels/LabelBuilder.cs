namespace Observability.Abstractions;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;


/// <summary>
/// Allow creation of labels from an object context.
/// </summary>
public class ContextLabelBuilder
{
    const char separator = '.';
    private readonly bool _ignoreNullValues;

    /// <summary>
    /// Creates a new instance of <see cref="ContextLabelBuilder"/>.
    /// </summary>
    /// <param name="ignoreNullValues"></param>
    public ContextLabelBuilder(bool ignoreNullValues = true)
    {
        _ignoreNullValues = ignoreNullValues;
    }

    /// <summary>
    /// Converts an object of type T into a dictionary of labels.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="context"></param>
    /// <param name="prefix">Prefix for dintionary keys</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public IDictionary<string, object?> LabelContext<T>(T context, string? prefix = null)
    {
        prefix = !string.IsNullOrEmpty(prefix) ? prefix.TrimEnd(separator) + separator : "";

        if (EqualityComparer<T>.Default.Equals(context, default(T)))
            throw new ArgumentNullException(nameof(context));

        var type = typeof(T);

        if (type.IsGenericType)
            throw new ArgumentException(message: $"El tipo {type.Name} es un tipo genérico", paramName: "type T");

        if (context is JsonElement json)
            return JsonExtensions.JsonToDictionary(json, prefix)
                .OrderBy(context => context.Key)
                .ToDictionary();

        if (type == typeof(string))
        {
            var result2 = new Dictionary<string, object?>();
            result2[prefix + type.Name.ToLowerInvariant()] = context;
            return result2;
        }

        var result = LabelContextType(type, context!, prefix, _ignoreNullValues);

        return result
            .OrderBy(context => context.Key)
            .ToDictionary();
    }

    private static IDictionary<string, object?> LabelContextType(Type type, object context, string prefix, bool ignoreNullValues)
    {
        var result = new ConcurrentDictionary<string, object?>();

        if (type.IsPrimitive || type == typeof(string)) return result.ToDictionary(); //Protect from go inside primitive types

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // Si la propiedad es simple se añade, sino se llama a la función recursivamente
            if (prop.PropertyType.IsPrimitive || prop.PropertyType == typeof(string))
            {
                SetDictionaryProperty(context, ignoreNullValues, result, prop, prefix);
            }
            else if (
                (prop.PropertyType.IsClass || prop.PropertyType.IsInterface)
                && prop.PropertyType != typeof(string))  // Si la propiedad es un objeto, se llama a la función recursivamente
            {
                ProcessProperty(context, result, prop, prefix, ignoreNullValues);
            }
            else
            {
                SetDictionaryProperty(context, ignoreNullValues, result, prop, "");
            }
        }

        return result.ToDictionary()
            .OrderBy(context => context.Key)
            .ToDictionary(); ;
    }

    private static void SetDictionaryProperty(object context, bool ignoreNullValues, ConcurrentDictionary<string, object?> result, PropertyInfo prop, string prefix = "")
    {
        var propertyName = prop.Name.ToLowerInvariant();
        var propertyValue = prop.GetValue(context);
        if (propertyValue == null && ignoreNullValues) return;
        var val = propertyValue?.ToString();

        result.AddOrUpdate(prefix + propertyName, val, (key, old) => val);
    }
    private static void SetDictionaryPropertyValue(object? value, bool ignoreNullValues, ConcurrentDictionary<string, object?> result, PropertyInfo prop, string prefix = "")
    {
        var propertyName = prop.Name.ToLowerInvariant();
        var propertyValue = value;
        if (propertyValue == null && ignoreNullValues) return;
        var val = propertyValue?.ToString();

        result.AddOrUpdate(prefix + propertyName, val, (key, old) => val);
    }

    private static void ProcessProperty<T>(T context, 
        ConcurrentDictionary<string, object?> result, 
        PropertyInfo prop, string prefix, bool ignoreNullValues) where T : class
    {
        if (prop.PropertyType == typeof(object)) //Prevent deep inspection
        {
            //Si no es un objeto real y es string convertir en Tag
            var propertyValue = prop.GetValue(context);
            if (propertyValue == null || propertyValue is string)
                SetDictionaryPropertyValue(propertyValue, ignoreNullValues, result, prop, "");
        }

        //Si alguna de las propiedades tiene el atributo SerializableLabelAttribute, se añade directamente
        if (prop.GetCustomAttributes(typeof(SerializableLabelAttribute), true).Length > 0)
        {
            SetSerializableProperty(context, result, prop);
        }
        else
        {
            var subContext = prop.GetValue(context);
            if (subContext != null)
            {
                var subLabels = LabelContextType(subContext.GetType(), subContext, "", ignoreNullValues);
                foreach (var subLabel in subLabels)
                {
                    // Se añade el prefijo de la propiedad padre
                    result.AddOrUpdate($"{prefix}{prop.Name.ToLowerInvariant()}{separator}{subLabel.Key}", subLabel.Value, (key, old) => subLabel.Value);
                }
            }
        }
    }

    private static void SetSerializableProperty<T>(T context, ConcurrentDictionary<string, object?> result, PropertyInfo prop) where T : class
    {
        string? value = null;

        if (prop.PropertyType.IsArray)
        {
            var array = prop.GetValue(context) as Array;
            if (array != null)
            {
                value = string.Join(",", array.Cast<object>().Select(x => x?.ToString() ?? ""));
            }
        }
        else if (prop.PropertyType == typeof(string[]))
        {
            value = string.Join(",", (string[])(prop.GetValue(context) ?? Array.Empty<string>()));
        }
        else if (prop.PropertyType == typeof(string))
        {
            value = prop.GetValue(context)?.ToString() ?? "";
        }

        result.AddOrUpdate(prop.Name.ToLowerInvariant(), value, (key, old) => value);
    }
}
