namespace Observability.Tests.Abstractions.Metrics;

using AutoFixture;
using System.ComponentModel;
using Observability.Abstractions;
using Xunit;
using Moq;

public class MetricsServiceTests
{
    private readonly Fixture _fixture = new Fixture();

    [Fact]
    [DisplayName("Debe invocar Register con los parámetros correctos")]
    public void ShouldInvokeRegisterWithCorrectParameters()
    {
        // Arrange
        var mockService = new Mock<IMetricsService>();
        var metric = new MetricContext
        {
            Name = _fixture.Create<string>(),
            Type = MetricInstrumentType.Counter
        };
        var value = _fixture.Create<double>();

        // Act
        mockService.Object.Register(metric, value);

        // Assert
        mockService.Verify(s => s.Register(metric, value), Times.Once);
    }

    [Fact]
    [DisplayName("Debe permitir múltiples llamadas a Register")]
    public void ShouldAllowMultipleRegisterCalls()
    {
        // Arrange
        var mockService = new Mock<IMetricsService>();
        var metric1 = new MetricContext { Name = "Metric1", Type = MetricInstrumentType.Gauge };
        var metric2 = new MetricContext { Name = "Metric2", Type = MetricInstrumentType.Histogram };

        // Act
        mockService.Object.Register(metric1, 10.5);
        mockService.Object.Register(metric2, 99.9);

        // Assert
        mockService.Verify(s => s.Register(metric1, 10.5), Times.Once);
        mockService.Verify(s => s.Register(metric2, 99.9), Times.Once);
    }

    [Fact]
    [DisplayName("Debe capturar los parámetros pasados a Register")]
    public void ShouldCaptureParametersPassedToRegister()
    {
        // Arrange
        var mockService = new Mock<IMetricsService>();
        MetricContext capturedMetric = null!;
        double capturedValue = 0;

        mockService.Setup(s => s.Register(It.IsAny<MetricContext>(), It.IsAny<double>()))
                   .Callback<MetricContext, double>((m, v) =>
                   {
                       capturedMetric = m;
                       capturedValue = v;
                   });

        var metric = new MetricContext { Name = "CapturedMetric", Type = MetricInstrumentType.Counter };
        var value = 42.0;

        // Act
        mockService.Object.Register(metric, value);

        // Assert
        Assert.Equal(metric, capturedMetric);
        Assert.Equal(value, capturedValue);
    }
}
