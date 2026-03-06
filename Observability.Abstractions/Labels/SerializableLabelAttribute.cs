namespace Observability.Abstractions;

using System;

/// <summary>
/// Indicates that a property is a serializable metric.
/// </summary>
/// <remarks>This attribute is used to mark properties that represent metrics which can be serialized. It should
/// be applied to properties only.</remarks>
[AttributeUsage(AttributeTargets.Property)]
public class SerializableLabelAttribute : Attribute
{
}
