namespace Observability.Tests.Abstractions.Labels;

using System.ComponentModel;
using Observability.Abstractions;
using Xunit;

using System;
using System.Linq;

public class SerializableMetricAttributeTests
{
    [Fact]
    [DisplayName("Debe poder instanciar SerializableMetricAttribute")]
    public void ShouldInstantiateAttribute()
    {
        // Act
        var attribute = new SerializableLabelAttribute();

        // Assert
        Assert.NotNull(attribute);
        Assert.IsType<SerializableLabelAttribute>(attribute);
    }

    [Fact]
    [DisplayName("Debe tener AttributeUsage configurado para propiedades")]
    public void ShouldHaveAttributeUsageForProperties()
    {
        // Act
        var usage = typeof(SerializableLabelAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        Assert.NotNull(usage);
        Assert.Equal(AttributeTargets.Property, usage.ValidOn);
    }

    [Fact]
    [DisplayName("Debe poder aplicarse a una propiedad y detectarse por reflexión")]
    public void ShouldApplyToPropertyAndBeDetected()
    {
        // Arrange
        var property = typeof(TestClass).GetProperty(nameof(TestClass.Metric));

        // Act
        var attribute = property!.GetCustomAttributes(typeof(SerializableLabelAttribute), false)
                                .FirstOrDefault();

        // Assert
        Assert.NotNull(attribute);
        Assert.IsType<SerializableLabelAttribute>(attribute);
    }

    private class TestClass
    {
        [SerializableLabel]
        public int Metric { get; set; }
    }
}
