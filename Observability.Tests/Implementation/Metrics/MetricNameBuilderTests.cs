namespace Observability.Tests.Implementation.AppCard;

using System;
using System.ComponentModel;
using AutoFixture;
using Xunit;

public class MetricNameBuilderTests
{
    private readonly Fixture _fixture = new Fixture();

    [Fact]
    [DisplayName("Debe generar nombre sin prefijo cuando no se especifica")]
    public void GetMetricName_ShouldGenerateNameWithoutPrefix()
    {
        // Arrange
        var builder = new MetricNameBuilder();
        var names = new[] { "Area", "Proyecto", "App" };

        // Act
        var result = builder.NormalizeName(names);

        // Assert
        Assert.Equal("area.proyecto.app", result);
    }

    [Fact]
    [DisplayName("Debe generar nombre con prefijo cuando se especifica")]
    public void GetMetricName_ShouldGenerateNameWithPrefix()
    {
        // Arrange
        var builder = new MetricNameBuilder("prefix");
        var names = new[] { "Area", "Proyecto" };

        // Act
        var result = builder.NormalizeName(names);

        // Assert
        Assert.Equal("prefix.area.proyecto", result);
    }

    [Fact]
    [DisplayName("Debe reemplazar espacios y guiones bajos por puntos")]
    public void GetMetricName_ShouldReplaceSpacesAndUnderscores()
    {
        // Arrange
        var builder = new MetricNameBuilder();
        var names = new[] { "Area Proyecto", "Sub_Area" };

        // Act
        var result = builder.NormalizeName(names);

        // Assert
        Assert.Equal("area.proyecto.sub.area", result);
    }

    [Fact]
    [DisplayName("Debe lanzar excepción si names es nulo o vacío")]
    public void GetMetricName_ShouldThrowIfNamesIsNullOrEmpty()
    {
        // Arrange
        var builder = new MetricNameBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.NormalizeName(null!));
        Assert.Throws<ArgumentNullException>(() => builder.NormalizeName(Array.Empty<string>()));
    }

    [Fact]
    [DisplayName("Debe ignorar valores nulos o vacíos en names")]
    public void GetMetricName_ShouldIgnoreNullOrEmptyValues()
    {
        // Arrange
        var builder = new MetricNameBuilder();
        var names = new string[] { "Area", "", "Proyecto" };

        // Act
        var result = builder.NormalizeName(names);

        // Assert
        Assert.Equal("area.proyecto", result);
    }
}
