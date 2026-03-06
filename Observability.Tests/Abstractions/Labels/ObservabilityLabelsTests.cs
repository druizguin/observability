namespace Observability.Tests.Abstractions.Labels;

using System.Collections.Generic;
using System.ComponentModel;
using AutoFixture;
using Moq;
using Observability.Abstractions;
using Xunit;

public class ObservabilityLabelsTests
{
    private readonly Fixture _fixture = new Fixture();

    [Fact]
    [DisplayName("Debe devolver el diccionario Labels configurado en el mock")]
    public void ShouldReturnConfiguredLabelsDictionary()
    {
        // Arrange
        var expectedLabels = new Dictionary<string, object?>
        {
            { "Key1", "Value1" },
            { "Key2", 123 }
        };

        var mock = new Mock<IObservabilityLabels>();
        mock.SetupGet(x => x.Labels).Returns(expectedLabels);

        // Act
        var result = mock.Object.Labels;

        // Assert
        Assert.Equal(expectedLabels, result);
        Assert.Equal("Value1", result["Key1"]);
        Assert.Equal(123, result["Key2"]);
    }

    [Fact]
    [DisplayName("Debe permitir agregar elementos al diccionario Labels")]
    public void ShouldAllowAddingItemsToLabels()
    {
        // Arrange
        var labels = new Dictionary<string, object?>();
        var mock = new Mock<IObservabilityLabels>();
        mock.SetupGet(x => x.Labels).Returns(labels);

        var key = _fixture.Create<string>();
        var value = _fixture.Create<string>();

        // Act
        mock.Object.Labels[key] = value;

        // Assert
        Assert.True(mock.Object.Labels.ContainsKey(key));
        Assert.Equal(value, mock.Object.Labels[key]);
    }

    [Fact]
    [DisplayName("Debe devolver un diccionario vacío si no se configuró nada")]
    public void ShouldReturnEmptyDictionaryIfNotConfigured()
    {
        // Arrange
        var mock = new Mock<IObservabilityLabels>();
        mock.SetupGet(x => x.Labels).Returns(new Dictionary<string, object?>());

        // Act
        var result = mock.Object.Labels;

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    [DisplayName("Debe verificar que se accede a la propiedad Labels")]
    public void ShouldVerifyLabelsPropertyAccess()
    {
        // Arrange
        var mock = new Mock<IObservabilityLabels>();
        mock.SetupGet(x => x.Labels).Returns(new Dictionary<string, object?>());

        // Act
        var _ = mock.Object.Labels;

        // Assert
        mock.VerifyGet(x => x.Labels, Times.Once);
    }
}
