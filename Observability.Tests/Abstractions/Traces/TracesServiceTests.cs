namespace Observability.Tests.Abstractions.Traces;


using System.ComponentModel;
using AutoFixture;
using Moq;
using Xunit;
using Observability.Abstractions;
using System.Diagnostics;

public class TracesServiceTests
{
    private readonly Fixture _fixture = new Fixture();

    [Fact]
    [DisplayName("Debe devolver la actividad configurada en el mock")]
    public void GetCurrentActivity_ShouldReturnConfiguredActivity()
    {
        // Arrange
        var expectedActivity = new Activity("TestActivity");
        var mock = new Mock<ITracesService>();
        mock.Setup(s => s.GetCurrentActivity(It.IsAny<TraceBuilder>())).Returns(expectedActivity);

        var builder = new TraceBuilder("Trace1");

        // Act
        var result = mock.Object.GetCurrentActivity(builder);

        // Assert
        Assert.Same(expectedActivity, result);
    }

    [Fact]
    [DisplayName("Debe invocar GetCurrentActivity con el TraceBuilder correcto")]
    public void GetCurrentActivity_ShouldBeCalledWithCorrectBuilder()
    {
        // Arrange
        var mock = new Mock<ITracesService>();
        var builder = new TraceBuilder("TraceX");

        // Act
        mock.Object.GetCurrentActivity(builder);

        // Assert
        mock.Verify(s => s.GetCurrentActivity(builder), Times.Once);
    }

    [Fact]
    [DisplayName("Debe devolver el proceso configurado en RegisterActivity")]
    public void RegisterActivity_ShouldReturnConfiguredProcess()
    {
        // Arrange
        var expectedProcess = new Mock<IActivityProcess>().Object;
        var mock = new Mock<ITracesService>();
        mock.Setup(s => s.RegisterActivity(It.IsAny<TraceBuilder>())).Returns(expectedProcess);

        var builder = new TraceBuilder("TraceY");

        // Act
        var result = mock.Object.RegisterActivity(builder);

        // Assert
        Assert.Same(expectedProcess, result);
    }

    [Fact]
    [DisplayName("Debe capturar el TraceBuilder pasado a RegisterActivity")]
    public void RegisterActivity_ShouldCaptureBuilderParameter()
    {
        // Arrange
        var mock = new Mock<ITracesService>();
        TraceBuilder capturedBuilder = null!;

        mock.Setup(s => s.RegisterActivity(It.IsAny<TraceBuilder>()))
            .Callback<TraceBuilder>(b => capturedBuilder = b)
            .Returns(new Mock<IActivityProcess>().Object);

        var builder = new TraceBuilder("CapturedTrace");

        // Act
        mock.Object.RegisterActivity(builder);

        // Assert
        Assert.NotNull(capturedBuilder);
        Assert.Equal("CapturedTrace", capturedBuilder.Name);
    }
}
