namespace Observability.Tests.Abstractions.Extensions;

using AutoFixture;
using System.ComponentModel;
using Observability.Abstractions;
using Xunit;
using System.Text.Json;

public class JsonExtensionsTests
{
    private readonly Fixture _fixture = new Fixture();

    [Fact]
    [DisplayName("FlattenJson debe convertir un objeto JSON simple en pares clave-valor")]
    public void FlattenJson_ShouldFlattenSimpleJson()
    {
        // Arrange
        var json = "{\"Name\":\"John\",\"Age\":30}";
        var element = JsonDocument.Parse(json).RootElement;

        // Act
        var result = JsonExtensions.JsonToDictionary(element);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, kvp => kvp.Key == "Name" && kvp.Value?.ToString() == "John");
        Assert.Contains(result, kvp => kvp.Key == "Age" && kvp.Value?.ToString() == "30");
    }

    [Fact]
    [DisplayName("FlattenJson debe manejar objetos anidados")]
    public void FlattenJson_ShouldHandleNestedObjects()
    {
        // Arrange
        var json = "{\"Person\":{\"Name\":\"Alice\",\"Address\":{\"City\":\"Madrid\"}}}";
        var element = JsonDocument.Parse(json).RootElement;

        // Act
        var result = JsonExtensions.JsonToDictionary(element);

        // Assert
        Assert.Contains(result, kvp => kvp.Key == "Person.Name" && kvp.Value?.ToString() == "Alice");
        Assert.Contains(result, kvp => kvp.Key == "Person.Address.City" && kvp.Value?.ToString() == "Madrid");
    }

    [Fact]
    [DisplayName("FlattenJson debe manejar arrays")]
    public void FlattenJson_ShouldHandleArrays()
    {
        // Arrange
        var json = "{\"Numbers\":[1,2,3]}";
        var element = JsonDocument.Parse(json).RootElement;

        // Act
        var result = JsonExtensions.JsonToDictionary(element);

        // Assert
        Assert.Contains(result, kvp => kvp.Key == "Numbers[0]" && kvp.Value?.ToString() == "1");
        Assert.Contains(result, kvp => kvp.Key == "Numbers[1]" && kvp.Value?.ToString() == "2");
        Assert.Contains(result, kvp => kvp.Key == "Numbers[2]" && kvp.Value?.ToString() == "3");
    }

    [Fact]
    [DisplayName("JsonToDictionary debe devolver un diccionario con claves planas")]
    public void JsonToDictionary_ShouldReturnFlattenedDictionary()
    {
        // Arrange
        var json = "{\"User\":{\"Id\":1,\"Roles\":[\"Admin\",\"User\"]}}";
        var element = JsonDocument.Parse(json).RootElement;

        // Act
        var result = JsonExtensions.JsonToDictionary(element);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("1", result["User.Id"]?.ToString());
        Assert.Equal("Admin", result["User.Roles[0]"]?.ToString());
        Assert.Equal("User", result["User.Roles[1]"]?.ToString());
    }

    [Fact]
    [DisplayName("FlattenJson debe manejar JSON vacío")]
    public void FlattenJson_ShouldHandleEmptyJson()
    {
        // Arrange
        var json = "{}";
        var element = JsonDocument.Parse(json).RootElement;

        // Act
        var result = JsonExtensions.JsonToDictionary(element);

        // Assert
        Assert.Empty(result);
    }
}
