namespace Observability.Tests.Abstractions.Metrics.Dom;

using AutoFixture;
using Moq;
using System.ComponentModel;
using System.Reflection;
using Observability.Abstractions;
using Xunit;

public class MetricBuilderTests
{
    private readonly Fixture _fixture = new Fixture();

    [Fact]
    [DisplayName("Debe inicializar Labels como un diccionario vacío")]
    public void ShouldInitializeLabelsAsEmptyDictionary()
    {
        // Act
        var builder = new MetricBuilder();

        // Assert
        Assert.NotNull(builder.Labels);
        Assert.Empty(builder.Labels);
    }

    [Fact]
    [DisplayName("Debe permitir asignar y leer propiedades heredadas")]
    public void ShouldSetAndGetInheritedProperties()
    {
        // Arrange
        var name = _fixture.Create<string>();
        var description = _fixture.Create<string>();
        var unit = _fixture.Create<string>();
        var type = MetricInstrumentType.Counter;

        var builder = new MetricBuilder
        {
            Name = name,
            Description = description,
            Unit = unit,
            Type = type
        };

        // Assert
        Assert.Equal(name, builder.Name);
        Assert.Equal(description, builder.Description);
        Assert.Equal(unit, builder.Unit);
        Assert.Equal(type, builder.Type);
    }

    [Fact]
    [DisplayName("Debe permitir agregar etiquetas al diccionario Labels")]
    public void ShouldAllowAddingLabels()
    {
        // Arrange
        var builder = new MetricBuilder();
        var key = _fixture.Create<string>();
        var value = _fixture.Create<string>();

        // Act
        builder.Labels[key] = value;

        // Assert
        Assert.True(builder.Labels.ContainsKey(key));
        Assert.Equal(value, builder.Labels[key]);
    }

    [Fact]
    [DisplayName("Service debe ser null por defecto")]
    public void ServiceShouldBeNullByDefault()
    {
        // Act
        var builder = new MetricBuilder();

        // Assert
        var serviceProperty = typeof(MetricBuilder).GetProperty("Service", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(serviceProperty);
        var value = serviceProperty?.GetValue(builder);
        Assert.Null(value);
    }

    [Fact]
    [DisplayName("Debe permitir asignar Service mediante reflexión")]
    public void ShouldAllowSettingServiceViaReflection()
    {
        // Arrange
        var builder = new MetricBuilder();
        var mockService = new Mock<IMetricsService>().Object;

        var serviceProperty = typeof(MetricBuilder).GetProperty("Service", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        serviceProperty!.SetValue(builder, mockService);
        var value = serviceProperty.GetValue(builder);

        // Assert
        Assert.NotNull(value);
        Assert.Same(mockService, value);
    }
}
