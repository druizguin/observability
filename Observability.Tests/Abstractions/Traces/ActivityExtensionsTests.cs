namespace Observability.Tests.Abstractions.Traces;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using AutoFixture;
using Moq;
using Observability.Abstractions;
using Xunit;


public class ActivityExtensionsTests
{
    private readonly Fixture _fixture = new();

    [Fact]
    [DisplayName("Devuelve null si la Activity es null y no invoca al nameBuilder")]
    public void SetTagsFromDictionary_ReturnsNull_WhenActivityIsNull()
    {
        // Arrange
        Activity? activity = null;
        var tags = new Dictionary<string, object?> { { "k1", "v1" } };
        var nameBuilderMock = new Mock<ILabelNameBuilder>(MockBehavior.Strict);

        // Act
        var result = ActivityExtensions.SetTagsFromDictionary(activity, tags, nameBuilderMock.Object);

        // Assert
        Assert.Null(result);
        nameBuilderMock.Verify(
            nb => nb.NormalizeName(It.IsAny<string>()),
            Times.Never,
            "No debería invocarse el nameBuilder si la Activity es null");
    }

    [Fact]
    [DisplayName("Lanza ArgumentNullException si tags es null")]
    public void SetTagsFromDictionary_Throws_WhenTagsIsNull()
    {
        // Arrange
        var activity = new Activity("test").Start();
        IDictionary<string, object?>? tags = null;
        var nameBuilderMock = new Mock<ILabelNameBuilder>(MockBehavior.Loose);

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() =>
            ActivityExtensions.SetTagsFromDictionary(activity, tags!, nameBuilderMock.Object));
    }

    [Fact]
    [DisplayName("No establece tags con valores nulos y sí los no nulos")]
    public void SetTagsFromDictionary_SetsOnlyNonNullTags()
    {
        // Arrange
        var activity = new Activity("test").Start();

        var tags = new Dictionary<string, object?>
        {
            { "nonNull1", "value1" },
            { "nonNull2", 123 },      // será convertido a string por ToString()
            { "nullValue", null }
        };

        var nameBuilderMock = new Mock<ILabelNameBuilder>(MockBehavior.Strict);
        nameBuilderMock
            .Setup(nb => nb.NormalizeName("nonNull1"))
            .Returns("metric.nonNull1");
        nameBuilderMock
            .Setup(nb => nb.NormalizeName("nonNull2"))
            .Returns("metric.nonNull2");
        nameBuilderMock
            .Setup(nb => nb.NormalizeName("nullValue"))
            .Returns("metric.nullValue");

        // Act
        var result = ActivityExtensions.SetTagsFromDictionary(activity, tags, nameBuilderMock.Object);

        // Assert
        Assert.Same(activity, result);

        // En .NET, Activity expone GetTagItem para recuperar valores; si no existe, devuelve null
        Assert.Equal("value1", activity.GetTagItem("nonNull1") as string);
        Assert.Equal("123", activity.GetTagItem("nonNull2") as string); // convertido a string
        Assert.Null(activity.GetTagItem("nullValue")); // no debería existir

        // Verificamos que nameBuilder fue llamado para cada entrada, incluida la nula (porque el código llama antes de filtrar al set)
        // Nota: el método filtra con Where(p => p.Value != null) ANTES de llamar SetTag,
        // pero llama GetMetricName ANTES del SetTag dentro del foreach de filtrados.
        // Con el código proporcionado, GetMetricName se ejecuta sólo para valores no nulos.
        nameBuilderMock.Verify(nb => nb.NormalizeName("nonNull1"), Times.Once);
        nameBuilderMock.Verify(nb => nb.NormalizeName("nonNull2"), Times.Once);
        nameBuilderMock.Verify(nb => nb.NormalizeName("nullValue"), Times.Never);
    }

    [Fact]
    [DisplayName("Ignora el nombre transformado del nameBuilder y usa la clave original (comportamiento actual)")]
    public void SetTagsFromDictionary_UsesOriginalKey_NotTransformedKey()
    {
        // Arrange
        var activity = new Activity("test").Start();

        var originalKey = "Original.Key";
        var transformedKey = "Transformed.Key";

        var tags = new Dictionary<string, object?>
        {
            { originalKey, "value" }
        };

        var nameBuilderMock = new Mock<ILabelNameBuilder>(MockBehavior.Strict);
        nameBuilderMock
            .Setup(nb => nb.NormalizeName(originalKey))
            .Returns(transformedKey);

        // Act
        ActivityExtensions.SetTagsFromDictionary(activity, tags, nameBuilderMock.Object);

        // Assert
        // El método actual hace: activity.SetTag(item.Key, item.Value?.ToString());
        // Por lo tanto, se guarda bajo la clave original, no la transformada.
        Assert.Equal("value", activity.GetTagItem(originalKey) as string);
        Assert.Null(activity.GetTagItem(transformedKey)); // no existe bajo la clave transformada

        nameBuilderMock.Verify(nb => nb.NormalizeName(originalKey), Times.Once);
    }

    [Fact]
    [DisplayName("Convierte los valores a string al setear el tag")]
    public void SetTagsFromDictionary_ConvertsValuesToString()
    {
        // Arrange
        var activity = new Activity("test").Start();

        var complexValue = new { A = 1, B = "x" }; // ToString() devuelve el nombre de tipo por defecto (anónimo)
        var tags = new Dictionary<string, object?>
        {
            { "complex", complexValue }
        };

        var nameBuilderMock = new Mock<ILabelNameBuilder>(MockBehavior.Strict);
        nameBuilderMock
            .Setup(nb => nb.NormalizeName("complex"))
            .Returns("metric.complex");

        // Act
        ActivityExtensions.SetTagsFromDictionary(activity, tags, nameBuilderMock.Object);

        // Assert
        var stored = activity.GetTagItem("complex") as string;
        Assert.NotNull(stored);
        // La representación será algo como "{ A = 1, B = x }" para tipos anónimos en algunas versiones,
        // o "System.Object" dependiendo del tipo; validamos solo que existe y es string.
        Assert.IsType<string>(stored);
    }

    [Fact]
    [DisplayName("No modifica la Activity si el diccionario de tags está vacío")]
    public void SetTagsFromDictionary_DoesNothing_WhenTagsEmpty()
    {
        // Arrange
        var activity = new Activity("test").Start();
        var tags = new Dictionary<string, object?>();
        var nameBuilderMock = new Mock<ILabelNameBuilder>(MockBehavior.Strict);

        // Act
        var result = ActivityExtensions.SetTagsFromDictionary(activity, tags, nameBuilderMock.Object);

        // Assert
        Assert.Same(activity, result);
        Assert.Empty(activity.Tags); // no hay tags
        nameBuilderMock.Verify(
            nb => nb.NormalizeName(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    [DisplayName("Integra AutoFixture para generar datos de prueba con mezcla de valores nulos y no nulos")]
    public void SetTagsFromDictionary_WithAutoFixtureData()
    {
        // Arrange
        var activity = new Activity("test").Start();

        var keys = _fixture.CreateMany<string>(5).ToList();
        var tags = new Dictionary<string, object?>
        {
            { keys[0], _fixture.Create<string>() },
            { keys[1], _fixture.Create<int>() },
            { keys[2], null },
            { keys[3], _fixture.Create<Guid>() },
            { keys[4], null }
        };

        var nameBuilderMock = new Mock<ILabelNameBuilder>(MockBehavior.Strict);
        foreach (var k in keys)
        {
            // Solo esperamos llamadas para valores no nulos
            if (tags[k] is not null)
            {
                nameBuilderMock.Setup(nb => nb.NormalizeName(k)).Returns($"metric.{k}");
            }
        }

        // Act
        ActivityExtensions.SetTagsFromDictionary(activity, tags, nameBuilderMock.Object);

        // Assert: claves con valor no nulo presentes, nulas ausentes
        foreach (var kvp in tags)
        {
            var stored = activity.GetTagItem(kvp.Key);
            if (kvp.Value is null)
            {
                Assert.Null(stored);
                nameBuilderMock.Verify(nb => nb.NormalizeName(kvp.Key), Times.Never);
            }
            else
            {
                Assert.NotNull(stored);
                Assert.Equal(kvp.Value!.ToString(), stored as string);
                nameBuilderMock.Verify(nb => nb.NormalizeName(kvp.Key), Times.Once);
            }
        }
    }
}
