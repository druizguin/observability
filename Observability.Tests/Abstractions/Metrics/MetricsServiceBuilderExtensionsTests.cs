namespace Observability.Tests.Abstractions.Metrics;

using AutoFixture;
using System.ComponentModel;
using Observability.Abstractions;
using Xunit;
using Moq;

using System;
using System.Collections.Generic;

public class MetricsServiceBuilderExtensionsTests
{
    private readonly Fixture _fx = new();

    // -----------------------------
    // Helpers
    // -----------------------------
    private static IDictionary<string, object?> MakeTags(params (string k, object? v)[] pairs)
    {
        var d = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (k, v) in pairs) d[k] = v;
        return d;
    }

    // -----------------------------
    // Counter
    // -----------------------------

    [Fact]
    [DisplayName("Counter configura Name, Type, Description, Unit y Labels cuando se proporcionan tags")]
    public void Counter_Configures_All_Fields_With_Tags()
    {
        // Arrange
        var service = new Mock<IMetricsService>(MockBehavior.Strict).Object;
        var name = _fx.Create<string>();
        var description = _fx.Create<string>();
        var unit = _fx.Create<string>();
        var tags = MakeTags(("env", "prod"), ("region", "eu-west"));

        // Act
        var builder = service.Counter(name, description, unit, tags);

        // Assert
        Assert.Same(service, builder.Service);
        Assert.Equal(name, builder.Name);
        Assert.Equal(MetricInstrumentType.Counter, builder.Type);
        Assert.Equal(description, builder.Description);
        Assert.Equal(unit, builder.Unit);
        Assert.NotNull(builder.Labels);
        Assert.Equal(tags, builder.Labels);
    }

    [Fact]
    [DisplayName("Counter no asigna Labels cuando tags es null")]
    public void Counter_DoesNotSet_Labels_When_Tags_Null()
    {
        // Arrange
        var service = new Mock<IMetricsService>(MockBehavior.Strict).Object;
        var name = _fx.Create<string>();

        // Act
        var builder = service.Counter(name, tags: null);

        // Assert
        Assert.Empty(builder.Labels);
    }

    [Fact]
    [DisplayName("Counter.Up invoca Register con valor por defecto 1")]
    public void Counter_Up_Invokes_Register_DefaultValue()
    {
        // Arrange
        var serviceMock = new Mock<IMetricsService>(MockBehavior.Strict);
        var service = serviceMock.Object;
        var builder = service.Counter(_fx.Create<string>());

        serviceMock
            .Setup(s => s.Register(builder, 1))
            .Verifiable();

        // Act
        builder.Up(); // default 1

        // Assert
        serviceMock.Verify();
    }

    [Fact]
    [DisplayName("Counter.Up invoca Register con valor específico")]
    public void Counter_Up_Invokes_Register_SpecificValue()
    {
        // Arrange
        var serviceMock = new Mock<IMetricsService>(MockBehavior.Strict);
        var service = serviceMock.Object;
        var builder = service.Counter(_fx.Create<string>());

        var value = _fx.Create<double>();
        serviceMock
            .Setup(s => s.Register(builder, value))
            .Verifiable();

        // Act
        builder.Up(value);

        // Assert
        serviceMock.Verify();
    }

    // -----------------------------
    // UpDownCounter
    // -----------------------------

    [Fact]
    [DisplayName("UpDownCounter configura correctamente y preserva tags")]
    public void UpDownCounter_Configures_Correctly()
    {
        // Arrange
        var service = new Mock<IMetricsService>(MockBehavior.Strict).Object;
        var name = _fx.Create<string>();
        var tags = MakeTags(("k1", 1), ("k2", "v2"));

        // Act
        var builder = service.UpDownCounter(name, tags: tags);

        // Assert
        Assert.Same(service, builder.Service);
        Assert.Equal(name, builder.Name);
        Assert.Equal(MetricInstrumentType.CounterUpDown, builder.Type);
        Assert.Equal(tags, builder.Labels);
    }

    [Fact]
    [DisplayName("UpDownCounter.Record invoca Register con el valor especificado")]
    public void UpDownCounter_Record_Invokes_Register()
    {
        // Arrange
        var serviceMock = new Mock<IMetricsService>(MockBehavior.Strict);
        var service = serviceMock.Object;
        var builder = service.UpDownCounter(_fx.Create<string>());

        var value = _fx.Create<double>();
        serviceMock
            .Setup(s => s.Register(builder, value))
            .Verifiable();

        // Act
        builder.Record(value);

        // Assert
        serviceMock.Verify();
    }

    // -----------------------------
    // Gauge
    // -----------------------------

    [Fact]
    [DisplayName("Gauge configura correctamente incluyendo Description y Unit")]
    public void Gauge_Configures_Correctly_Including_Description_And_Unit()
    {
        // Arrange
        var service = new Mock<IMetricsService>(MockBehavior.Strict).Object;
        var name = _fx.Create<string>();
        var description = _fx.Create<string>();
        var unit = _fx.Create<string>();

        // Act
        var builder = service.Gauge(name, description, unit);

        // Assert
        Assert.Same(service, builder.Service);
        Assert.Equal(name, builder.Name);
        Assert.Equal(MetricInstrumentType.Gauge, builder.Type);
        Assert.Equal(description, builder.Description);
        Assert.Equal(unit, builder.Unit);
        Assert.Empty(builder.Labels);
    }

    [Fact]
    [DisplayName("Gauge.Record invoca Register con valor por defecto 1")]
    public void Gauge_Record_Invokes_Register_Default()
    {
        // Arrange
        var serviceMock = new Mock<IMetricsService>(MockBehavior.Strict);
        var service = serviceMock.Object;
        var builder = service.Gauge(_fx.Create<string>());

        serviceMock
            .Setup(s => s.Register(builder, 1))
            .Verifiable();

        // Act
        builder.Record();

        // Assert
        serviceMock.Verify();
    }

    // -----------------------------
    // Histogram
    // -----------------------------

    [Fact]
    [DisplayName("Histogram configura correctamente y asigna Labels cuando hay tags")]
    public void Histogram_Configures_Correctly_With_Tags()
    {
        // Arrange
        var service = new Mock<IMetricsService>(MockBehavior.Strict).Object;
        var name = _fx.Create<string>();
        var tags = MakeTags(("a", "b"));

        // Act
        var builder = service.Histogram(name, tags: tags);

        // Assert
        Assert.Same(service, builder.Service);
        Assert.Equal(name, builder.Name);
        Assert.Equal(MetricInstrumentType.Histogram, builder.Type);
        Assert.Equal(tags, builder.Labels);
    }

    [Fact]
    [DisplayName("Histogram.Record invoca Register con valor custom")]
    public void Histogram_Record_Invokes_Register_Custom()
    {
        // Arrange
        var serviceMock = new Mock<IMetricsService>(MockBehavior.Strict);
        var service = serviceMock.Object;
        var builder = service.Histogram(_fx.Create<string>());

        var value = 42.5;
        serviceMock
            .Setup(s => s.Register(builder, value))
            .Verifiable();

        // Act
        builder.Record(value);

        // Assert
        serviceMock.Verify();
    }

    // -----------------------------
    // Validación de argumentos (service/name)
    // -----------------------------

    [Fact]
    [DisplayName("Counter lanza ArgumentNullException si service es null")]
    public void Counter_Throws_When_Service_Null()
    {
        // Arrange
        IMetricsService? service = null;

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() =>
            service!.Counter(_fx.Create<string>()));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("    ")]
    [DisplayName("Counter lanza ArgumentException si name es vacío o espacios")]
    public void Counter_Throws_When_Name_Is_Empty_Or_Whitespace(string name)
    {
        // Arrange
        var service = new Mock<IMetricsService>(MockBehavior.Strict).Object;

        // Act + Assert
        Assert.Throws<ArgumentException>(() =>
            service.Counter(name));
    }

    [Fact]
    [DisplayName("Gauge.Record lanza si builder es null")]
    public void Gauge_Record_Throws_When_Builder_Null()
    {
        // Arrange
        GaugeMetricBuilder? builder = null;

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => builder!.Record());
    }

    [Fact]
    [DisplayName("Counter.Up lanza si builder.Service es null")]
    public void Counter_Up_Throws_When_Service_Null_On_Builder()
    {
        // Arrange
        var builder = new CounterMetricBuilder
        {
            // Sin Service para forzar la excepción
            Name = _fx.Create<string>(),
            Type = MetricInstrumentType.Counter
        };

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => builder.Up());
    }

    [Fact]
    [DisplayName("UpDownCounter.Record lanza si builder es null")]
    public void UpDownCounter_Record_Throws_When_Builder_Null()
    {
        // Arrange
        CounterUpDownMetricBuilder? builder = null;

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => builder!.Record());
    }

    [Fact]
    [DisplayName("Histogram.Record lanza si builder.Service es null")]
    public void Histogram_Record_Throws_When_Service_Null_On_Builder()
    {
        // Arrange
        var builder = new HistogramMetricBuilder
        {
            Name = _fx.Create<string>(),
            Type = MetricInstrumentType.Histogram
        };

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => builder.Record());
    }
}
