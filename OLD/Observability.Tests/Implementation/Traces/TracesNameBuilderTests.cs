namespace Observability.Tests.Implementation.Traces;
using System;
using System.ComponentModel;
using Xunit;



public class TracesNameBuilderTests
{
    [Fact]
    [DisplayName("Debe generar nombre con prefijo en TracesNameBuilder")]
    public void GetMetricName_ShouldGenerateNameWithPrefixInTraces()
    {
        // Arrange
        var builder = new LabelNameBuilder("tracePrefix");
        var names = new[] { "Trace", "Segment" };

        // Act
        var result = builder.NormalizeName(names);

        // Assert
        Assert.Equal("traceprefix.trace.segment", result);
    }

    [Fact]
    [DisplayName("Debe lanzar excepción si names es nulo o vacío en TracesNameBuilder")]
    public void GetMetricName_ShouldThrowIfNamesIsNullOrEmptyInTraces()
    {
        // Arrange
        var builder = new LabelNameBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.NormalizeName(null!));
        Assert.Throws<ArgumentNullException>(() => builder.NormalizeName(Array.Empty<string>()));
    }

    [Fact]
    [DisplayName("Debe normalizar nombres en minúsculas en TracesNameBuilder")]
    public void GetMetricName_ShouldNormalizeToLowerCaseInTraces()
    {
        // Arrange
        var builder = new LabelNameBuilder();
        var names = new[] { "TraceName", "SubTrace" };

        // Act
        var result = builder.NormalizeName(names);

        // Assert
        Assert.Equal("tracename.subtrace", result);
    }
}
