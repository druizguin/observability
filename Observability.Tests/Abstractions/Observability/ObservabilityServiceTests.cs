namespace Observability.Tests.Abstractions.Observability;

using AutoFixture;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.ComponentModel;
using Observability.Abstractions;
using Xunit;


public class ObservabilityServiceTests
{
    private readonly Fixture _fixture = new Fixture();

    [Fact]
    [DisplayName("Debe devolver instancias configuradas para Metrics, Traces y ApplicationCard")]
    public void ShouldReturnConfiguredInstances()
    {
        // Arrange
        var metricsMock = new Mock<IMetricsService>().Object;
        var tracesMock = new Mock<ITracesService>().Object;
        var appCardMock = new Mock<IApplicationCard>().Object;

        var serviceMock = new Mock<IObservabilityService>();
        serviceMock.SetupGet(s => s.Metrics).Returns(metricsMock);
        serviceMock.SetupGet(s => s.Traces).Returns(tracesMock);
        serviceMock.SetupGet(s => s.ApplicationCard).Returns(appCardMock);

        // Act
        var metrics = serviceMock.Object.Metrics;
        var traces = serviceMock.Object.Traces;
        var appCard = serviceMock.Object.ApplicationCard;

        // Assert
        Assert.Same(metricsMock, metrics);
        Assert.Same(tracesMock, traces);
        Assert.Same(appCardMock, appCard);
    }

    [Fact]
    [DisplayName("Debe verificar que se accede a la propiedad Metrics")]
    public void ShouldVerifyMetricsAccess()
    {
        // Arrange
        var serviceMock = new Mock<IObservabilityService>();
        serviceMock.SetupGet(s => s.Metrics).Returns(new Mock<IMetricsService>().Object);

        // Act
        var _ = serviceMock.Object.Metrics;

        // Assert
        serviceMock.VerifyGet(s => s.Metrics, Times.Once);
    }

    [Fact]
    [DisplayName("Debe verificar que se accede a la propiedad Traces")]
    public void ShouldVerifyTracesAccess()
    {
        // Arrange
        var serviceMock = new Mock<IObservabilityService>();
        serviceMock.SetupGet(s => s.Traces).Returns(new Mock<ITracesService>().Object);

        // Act
        var _ = serviceMock.Object.Traces;

        // Assert
        serviceMock.VerifyGet(s => s.Traces, Times.Once);
    }

    [Fact]
    [DisplayName("Debe verificar que se accede a la propiedad ApplicationCard")]
    public void ShouldVerifyApplicationCardAccess()
    {
        // Arrange
        var serviceMock = new Mock<IObservabilityService>();
        serviceMock.SetupGet(s => s.ApplicationCard).Returns(new Mock<IApplicationCard>().Object);

        // Act
        var _ = serviceMock.Object.ApplicationCard;

        // Assert
        serviceMock.VerifyGet(s => s.ApplicationCard, Times.Once);
    }

    [Fact]
    [DisplayName("Debe verificar que lanza excepcion cuando loggerFactory es nulo")]
    public void ShouldVerifyLoggerInConstructor()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            var service = new ObservabilityService(null!,
                new Mock<IOptions<IApplicationCard>>().Object,
                new Mock<ITracesService>().Object,
                new Mock<IMetricsService>().Object);
        });
       
    }

    [Fact]
    [DisplayName("Debe verificar que loggerFactory crea log")]
    public void ShouldVerifyLogger()
    {
        var appCardMock = new Mock<IOptions<IApplicationCard>>();
        appCardMock.Setup(a => a.Value).Returns(new ApplicationCard("test.arc.TestApp.test")
        {
            Version = "1.0.0",
            Entorno = "Development"
        });

        var service = new ObservabilityService(new NullLoggerFactory(),
            appCardMock.Object,
            new Mock<ITracesService>().Object,
            new Mock<IMetricsService>().Object);

        Assert.NotNull(service.Logger);
    }

    [Fact]
    [DisplayName("Debe verificar que loggerFactory crea log tipado")]
    public void ShouldVerifyLoggerTyped()
    {
        var appCardMock = new Mock<IOptions<IApplicationCard>>();
        appCardMock.Setup(a => a.Value).Returns(new ApplicationCard("test.arc.TestApp.test")
        {
            Version = "1.0.0",
            Entorno = "Development"
        });

        var service = new ObservabilityService<ApplicationCard>(new NullLoggerFactory().CreateLogger<ApplicationCard>(),
            appCardMock.Object,
            new Mock<ITracesService>().Object,
            new Mock<IMetricsService>().Object);

        Assert.NotNull(service.Logger);
        Assert.IsAssignableFrom<ILogger<ApplicationCard>>(service.Logger);
    }
}
