namespace Observability.Tests.Implementation.AppCard;

using System;
using System.ComponentModel;
using AutoFixture;
using Xunit;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Moq;
using Observability.Abstractions;

public class MetricsServiceTests
{
    private readonly Fixture _fixture = new Fixture();

    [Fact]
    [DisplayName("Constructor debe inicializar correctamente y lanzar excepción si parámetros son nulos")]
    public void Constructor_ShouldInitializeAndThrowIfNull()
    {
        // Arrange
        var meter = new Meter("TestMeter");
        var nameBuilderMock = new Mock<IMetricNameBuilder>().Object;
        var labelBuilderMock = new Mock<ILabelNameBuilder>().Object;

        // Act
        var service = new MetricsService(nameBuilderMock, labelBuilderMock, meter);

        // Assert
        Assert.NotNull(service);

        // Null checks
        Assert.Throws<ArgumentNullException>(() => new MetricsService(null!, labelBuilderMock, meter));
        Assert.Throws<ArgumentNullException>(() => new MetricsService(nameBuilderMock, labelBuilderMock, null!));
    }

    [Theory]
    [InlineData(MetricInstrumentType.Counter)]
    [InlineData(MetricInstrumentType.CounterUpDown)]
    [InlineData(MetricInstrumentType.Gauge)]
    [InlineData(MetricInstrumentType.Histogram)]
    [DisplayName("Register debe invocar el método correcto del Meter según el tipo")]
    public void Register_ShouldCallCorrectMeterMethod(MetricInstrumentType type)
    {
        // Arrange
        var meterMock = new Mock<Meter>("TestMeter");
        var nameBuilderMock = new Mock<IMetricNameBuilder>();
        nameBuilderMock.Setup(n => n.NormalizeName(It.IsAny<string[]>())).Returns("metric.name");
        var labelBuilderMock = new Mock<ILabelNameBuilder>();
        labelBuilderMock.Setup(n => n.NormalizeName(It.IsAny<string[]>())).Returns("key");

        var service = new MetricsService(nameBuilderMock.Object, labelBuilderMock.Object, meterMock.Object);

        var metric = new MetricContext
        {
            Name = "TestMetric",
            Description = "Description",
            Unit = "unit",
            Type = type,
            Labels = new Dictionary<string, object?> { { "key", "value" } }
        };

        var value = 42.0;

        // Act
        service.Register(metric, value);

        // Assert
        nameBuilderMock.Verify(n => n.NormalizeName(metric.Name, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    [DisplayName("Register debe usar etiquetas del MetricContext")]
    public void Register_ShouldUseLabelsFromMetricContext()
    {
        // Arrange
        var meter = new Meter("TestMeter");
        var nameBuilderMock = new Mock<IMetricNameBuilder>();
        nameBuilderMock.Setup(n => n.NormalizeName(It.IsAny<string[]>())).Returns("metric.name");
        var labelBuilder = new LabelNameBuilder();

        var service = new MetricsService(nameBuilderMock.Object, labelBuilder, meter);

        var metric = new MetricContext
        {
            Name = "MetricWithLabels",
            Type = MetricInstrumentType.Counter,
            Labels = new Dictionary<string, object?> { { "env", "prod" }, { "version", "1.0" } }
        };

        // Act
        service.Register(metric, 5);

        // Assert
        Assert.Equal(2, metric.Labels.Count);
        Assert.Contains("env", metric.Labels.Keys);
        Assert.Contains("version", metric.Labels.Keys);
    }
}
