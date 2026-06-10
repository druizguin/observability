namespace Observability.Tests.Abstractions.Traces;


using System.ComponentModel;
using AutoFixture;
using Moq;
using Xunit;
using Observability.Abstractions;

using System;
using System.Collections.Generic;

public class TracesNameBuilderTests
{
    private readonly Fixture _fixture = new Fixture();

    [Fact]
    [DisplayName("Debe devolver el nombre configurado en el mock")]
    public void ShouldReturnConfiguredMetricName()
    {
        // Arrange
        var expectedName = _fixture.Create<string>();
        var mock = new Mock<ILabelNameBuilder>();
        mock.Setup(b => b.NormalizeName(It.IsAny<string[]>())).Returns(expectedName);

        // Act
        var result = mock.Object.NormalizeName("part1", "part2");

        // Assert
        Assert.Equal(expectedName, result);
    }

    [Fact]
    [DisplayName("Debe invocar GetMetricName con los parámetros correctos")]
    public void ShouldInvokeGetMetricNameWithCorrectParameters()
    {
        // Arrange
        var mock = new Mock<ILabelNameBuilder>();
        var names = new[] { "segment1", "segment2", "segment3" };

        // Act
        mock.Object.NormalizeName(names);

        // Assert
        mock.Verify(b => b.NormalizeName(names), Times.Once);
    }

    [Fact]
    [DisplayName("Debe capturar los parámetros pasados a GetMetricName")]
    public void ShouldCaptureParametersPassedToGetMetricName()
    {
        // Arrange
        var mock = new Mock<ILabelNameBuilder>();
        string[] capturedNames = null!;

        mock.Setup(b => b.NormalizeName(It.IsAny<string[]>()))
            .Callback<string[]>(n => capturedNames = n)
            .Returns("Captured");

        var inputNames = new[] { "traceA", "traceB" };

        // Act
        var result = mock.Object.NormalizeName(inputNames);

        // Assert
        Assert.Equal("Captured", result);
        Assert.NotNull(capturedNames);
        Assert.Equal(inputNames, capturedNames);
    }
}
