namespace Observability.Tests.Abstractions.Extensions;

using System.Collections.Generic;
using System.ComponentModel;
using Observability.Abstractions;
using Xunit;

public class DictionaryExtensionsTests
{
    [Fact]
    [DisplayName("ToTags debe convertir el diccionario en un array de KeyValuePair")]
    public void ToTags_ShouldConvertDictionaryToArray()
    {
        // Arrange
        var dic = new Dictionary<string, object?>
        {
            { "Key1", "Value1" },
            { "Key2", 123 },
            { "Key3", null }
        };

        // Act
        var result = dic.ToTags();

        // Assert
        Assert.Equal(dic.Count, result.Length);
        Assert.Contains(result, kvp => kvp.Key == "Key1" && (string?)kvp.Value == "Value1");
        Assert.Contains(result, kvp => kvp.Key == "Key2" && (int?)kvp.Value == 123);
        Assert.Contains(result, kvp => kvp.Key == "Key3" && kvp.Value == null);
    }

    [Fact]
    [DisplayName("AddRange debe agregar elementos desde otro diccionario")]
    public void AddRange_ShouldAddItemsFromDictionary()
    {
        // Arrange
        var target = new Dictionary<string, string>
        {
            { "A", "1" }
        };

        var source = new Dictionary<string, string>
        {
            { "B", "2" },
            { "C", "3" }
        };

        // Act
        target.AddRange(source);

        // Assert
        Assert.Equal(3, target.Count);
        Assert.Equal("2", target["B"]);
        Assert.Equal("3", target["C"]);
    }

    [Fact]
    [DisplayName("AddRange debe agregar elementos desde KeyValuePair[]")]
    public void AddRange_ShouldAddItemsFromKeyValuePairs()
    {
        // Arrange
        var target = new Dictionary<string, int>
        {
            { "X", 10 }
        };

        var items = new[]
        {
            new KeyValuePair<string, int>("Y", 20),
            new KeyValuePair<string, int>("Z", 30)
        };

        // Act
        target.AddRange(items);

        // Assert
        Assert.Equal(3, target.Count);
        Assert.Equal(20, target["Y"]);
        Assert.Equal(30, target["Z"]);
    }

    [Fact]
    [DisplayName("AddRange debe agregar elementos desde tuplas")]
    public void AddRange_ShouldAddItemsFromTuples()
    {
        // Arrange
        var target = new Dictionary<string, bool>
        {
            { "Flag1", true }
        };

        var items = new[]
        {
            ("Flag2", false),
            ("Flag3", true)
        };

        // Act
        target.AddRange(items);

        // Assert
        Assert.Equal(3, target.Count);
        Assert.False(target["Flag2"]);
        Assert.True(target["Flag3"]);
    }

    [Fact]
    [DisplayName("AddRange debe sobrescribir valores existentes")]
    public void AddRange_ShouldOverwriteExistingValues()
    {
        // Arrange
        var target = new Dictionary<string, int>
        {
            { "Key", 1 }
        };

        var source = new Dictionary<string, int>
        {
            { "Key", 99 }
        };

        // Act
        target.AddRange(source);

        // Assert
        Assert.Equal(99, target["Key"]);
    }
}

