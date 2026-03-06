namespace Observability.Tests.Abstractions.Metrics;

using AutoFixture;
using System.ComponentModel;
using Observability.Abstractions;
using Xunit;

using System;

public class MetricsServiceExtensionsTests
{
    private readonly Fixture _fixture = new Fixture();

    [Fact]
    [DisplayName("WithDescription debe asignar la descripción y devolver la misma instancia")]
    public void WithDescription_ShouldSetDescriptionAndReturnSameInstance()
    {
        // Arrange
        var builder = new MetricBuilder();
        var description = _fixture.Create<string>();

        // Act
        var result = builder.WithDescription(description);

        // Assert
        Assert.Equal(description, builder.Description);
        Assert.Same(builder, result);
    }

    [Fact]
    [DisplayName("WithUnit debe asignar la unidad y devolver la misma instancia")]
    public void WithUnit_ShouldSetUnitAndReturnSameInstance()
    {
        // Arrange
        var builder = new MetricBuilder();
        var unit = _fixture.Create<string>();

        // Act
        var result = builder.WithUnit(unit);

        // Assert
        Assert.Equal(unit, builder.Unit);
        Assert.Same(builder, result);
    }

    [Fact]
    [DisplayName("WithDescription debe lanzar ArgumentNullException si builder es null")]
    public void WithDescription_ShouldThrowIfBuilderIsNull()
    {
        // Arrange
        MetricBuilder builder = null!;
        var description = _fixture.Create<string>();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => builder.WithDescription(description));
        Assert.Equal("builder", ex.ParamName);
    }

    [Fact]
    [DisplayName("WithUnit debe lanzar ArgumentNullException si builder es null")]
    public void WithUnit_ShouldThrowIfBuilderIsNull()
    {
        // Arrange
        MetricBuilder builder = null!;
        var unit = _fixture.Create<string>();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => builder.WithUnit(unit));
        Assert.Equal("builder", ex.ParamName);
    }
}
