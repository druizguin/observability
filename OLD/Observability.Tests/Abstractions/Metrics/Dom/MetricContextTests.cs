namespace Observability.Tests.Abstractions.Metrics.Dom;

using AutoFixture;
using System.ComponentModel;
using Observability.Abstractions;
using Xunit;

using System;

public class MetricContextTests
{
    private readonly Fixture _fixture = new Fixture();

    [Fact]
    [DisplayName("Debe inicializar Labels como un diccionario vacío")]
    public void ShouldInitializeLabelsAsEmptyDictionary()
    {
        // Act
        var context = new MetricContext();

        // Assert
        Assert.NotNull(context.Labels);
        Assert.Empty(context.Labels);
    }

    [Fact]
    [DisplayName("Debe permitir agregar elementos al diccionario Labels")]
    public void ShouldAllowAddingItemsToLabels()
    {
        // Arrange
        var context = new MetricContext();
        var key = _fixture.Create<string>();
        var value = _fixture.Create<string>();

        // Act
        context.Labels[key] = value;

        // Assert
        Assert.True(context.Labels.ContainsKey(key));
        Assert.Equal(value, context.Labels[key]);
    }

    [Fact]
    [DisplayName("Debe permitir asignar y leer propiedades Name, Description, Unit y Type")]
    public void ShouldSetAndGetProperties()
    {
        // Arrange
        var name = _fixture.Create<string>();
        var description = _fixture.Create<string>();
        var unit = _fixture.Create<string>();
        var type = MetricInstrumentType.Histogram;

        var context = new MetricContext
        {
            Name = name,
            Description = description,
            Unit = unit,
            Type = type
        };

        // Assert
        Assert.Equal(name, context.Name);
        Assert.Equal(description, context.Description);
        Assert.Equal(unit, context.Unit);
        Assert.Equal(type, context.Type);
    }

    [Fact]
    [DisplayName("Debe permitir Labels con valores nulos")]
    public void ShouldAllowNullValuesInLabels()
    {
        // Arrange
        var context = new MetricContext();
        var key = _fixture.Create<string>();

        // Act
        context.Labels[key] = null;

        // Assert
        Assert.True(context.Labels.ContainsKey(key));
        Assert.Null(context.Labels[key]);
    }
}
