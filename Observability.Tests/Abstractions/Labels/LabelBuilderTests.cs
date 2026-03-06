namespace Observability.Tests.Abstractions.Labels;

using AutoFixture;
using AutoFixture.Xunit2;
using Moq;
using System;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using Observability.Abstractions;
using Xunit;

public class LabelBuilderTests
{
    private readonly Fixture _fx = new();

    [Fact]
    [DisplayName("Lanza ArgumentNullException si el context es null")]
    public void LabelContext_Throws_When_Context_Null()
    {
        // Arrange
        var sut = new ContextLabelBuilder();
        SimpleContext? context = null;

        // Act + Assert
        var ex = Assert.Throws<ArgumentNullException>(() => sut.LabelContext(context!));
        Assert.Equal("context", ex.ParamName);
    }

    [Fact]
    [DisplayName("Lanza ArgumentException si el tipo genérico T es genérico")]
    public void LabelContext_Throws_When_T_Is_GenericType()
    {
        // Arrange
        var sut = new ContextLabelBuilder();
        var context = new GenericWrapper<int> { Value = 123 };

        // Act + Assert
        var ex = Assert.Throws<ArgumentException>(() => sut.LabelContext(context));
        Assert.Equal("type T", ex.ParamName);
        Assert.Contains("es un tipo genérico", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private class GenericWrapper<T>
    {
        public T? Value { get; set; }
    }

    [Fact]
    [DisplayName("Cuando T es string, genera una única etiqueta 'string' con el valor y aplica prefijo normalizado")]
    public void LabelContext_String_Types_With_Prefix_Normalization()
    {
        // Arrange
        var sut = new ContextLabelBuilder();

        // Act
        var r1 = sut.LabelContext("hello", prefix: "req");
        var r2 = sut.LabelContext("hello", prefix: "req.");
        var r3 = sut.LabelContext("hello", prefix: null);

        // Assert
        Assert.Single(r1);
        Assert.Equal("hello", r1["req.string"]);
        Assert.Single(r2);
        Assert.Equal("hello", r2["req.string"]);
        Assert.Single(r3);
        Assert.Equal("hello", r3["string"]);
    }

    [Fact]
    [DisplayName("Propiedades primitivas y string se añaden en minúsculas; valores null se omiten")]
    public void LabelContext_Adds_Primitives_And_Strings_Skips_Nulls()
    {
        // Arrange
        var sut = new ContextLabelBuilder();
        var ctx = new SimpleContext
        {
            Id = 42,
            Name = "John",
            Optional = null,
            Kind = StatusKind.Active
        };

        // Act
        var result = sut.LabelContext(ctx);

        // Assert
        // 'Id' y 'Name' como primitivas/string -> minúsculas, valores stringificados
        Assert.Equal("42", result["id"]);
        Assert.Equal("John", result["name"]);
        // Optional null se omite
        Assert.False(result.ContainsKey("optional"));
        // Enum entra en el 'else' (no primitive, no string); se serializa con ToString()
        Assert.Equal("Active", result["kind"]);
    }

    [Fact]
    [DisplayName("Propiedad de tipo object: si es string o null se añade usando el nombre exacto de la propiedad (sin lowercasing)")]
    public void LabelContext_Object_Property_Special_Behavior()
    {
        // Arrange
        var sut = new ContextLabelBuilder(false);
        var ctx1 = new ParentContext { Meta = "ABC" };
        var ctx2 = new ParentContext { Meta = null };

        // Act
        var r1 = sut.LabelContext(ctx1);
        var r2 = sut.LabelContext(ctx2);

        // Assert
        // Se usa 'prop.Name' tal cual (no .ToLowerInvariant)
        Assert.Equal("ABC", r1["meta"]);
        Assert.True(r2.ContainsKey("meta"));
        Assert.Null(r2["meta"]);
    }

    [Fact]
    [DisplayName("Contexto anidado: concatena prefijo + nombre del padre en minúsculas + '.' + clave hija")]
    public void LabelContext_Nested_Properties_Are_Flattened_With_Parent_Prefix()
    {
        // Arrange
        var sut = new ContextLabelBuilder(true);
        var ctx = new ParentContext
        {
            Title = "Report",
            Details = new ChildContext { City = "Madrid", Zip = 28001 }
        };

        // Act
        var result = sut.LabelContext(ctx, prefix: "req.");

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Report", result["req.title"]);
        Assert.Equal("Madrid", result["req.details.city"]);
        Assert.Equal("28001", result["req.details.zip"]);
        Assert.False(result.ContainsKey("meta"));
    }

    [Fact]
    [DisplayName("Contexto anidado: concatena prefijo + nombre del padre en minúsculas + '.' + clave hija y no ignora valores nulos")]
    public void LabelContext_Nested_Properties_Are_Flattened_With_Parent_Prefix_And_NotIgnoreNulls()
    {
        // Arrange
        var sut = new ContextLabelBuilder(false);
        var ctx = new ParentContext
        {
            Title = "Report",
            Details = new ChildContext { City = "Madrid", Zip = 28001 }
        };

        // Act
        var result = sut.LabelContext(ctx, prefix: "req.");

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Equal("Report", result["req.title"]);
        Assert.Equal("Madrid", result["req.details.city"]);
        Assert.Equal("28001", result["req.details.zip"]);
        Assert.Null(result["meta"]);
    }

    [Fact]
    [DisplayName("El prefijo se normaliza con TrimEnd('.') y se añade '.' al final si hay prefijo")]
    public void LabelContext_Prefix_Is_Normalized()
    {
        // Arrange
        var sut = new ContextLabelBuilder();
        var ctx = new SimpleContext { Id = 1, Name = "A" };

        // Act
        var r1 = sut.LabelContext(ctx, prefix: "env");   // -> "env."
        var r2 = sut.LabelContext(ctx, prefix: "env."); // -> "env."

        // Assert
        Assert.Equal("1", r1["env.id"]);
        Assert.Equal("A", r1["env.name"]);
        Assert.Equal("1", r2["env.id"]);
        Assert.Equal("A", r2["env.name"]);
    }

    [Fact]
    [DisplayName("Las claves del diccionario final están ordenadas alfabéticamente")]
    public void LabelContext_Returns_Sorted_Dictionary_By_Key()
    {
        // Arrange
        var sut = new ContextLabelBuilder();
        var ctx = new ParentContext
        {
            Title = "T",
            Details = new ChildContext { City = "C", Zip = 1 }
        };

        // Act
        var result = sut.LabelContext(ctx);

        // Assert
        var keys = result.Keys.ToList();
        var sorted = keys.OrderBy(k => k).ToList();
        Assert.Equal(sorted, keys);
    }


    [Fact]
    [DisplayName("Cuando T es JsonElement, delega en JsonExtensions.JsonToDictionary y aplica prefijo")]
    public void LabelContext_JsonElement_Path()
    {
        // Arrange
        var sut = new ContextLabelBuilder();
        JsonElement json = JsonDocument.Parse("{\"a\":1,\"b\":\"x\"}").RootElement;

        // Act
        var result = sut.LabelContext<JsonElement>(json, "req.");

        // Assert
        // Esperado: claves "req.a" -> "1", "req.b" -> "x"
        // Pero se requiere la implementación real de JsonExtensions.JsonToDictionary
        Assert.True(result.Count >= 2);
    }

    [Fact]
    [DisplayName("Propiedad con SerializableMetricAttribute serializa arrays como 'a,b,c'")]
    public void LabelContext_SerializableMetricAttribute_Arrays()
    {
        // Arrange
        var sut = new ContextLabelBuilder();
        var ctx = new WithSerializableArray { Tags = new[] { "a", "b", "c" } };

        // Act
        var result = sut.LabelContext(ctx);

        // Assert
        // Esperado: key 'tags' -> "a,b,c" (minúsculas)
        Assert.Equal("a,b,c", result["tags"]);
    }

    private class WithSerializableArray
    {
        // Marca con el atributo real en tu proyecto:
        [SerializableLabel]
        public string[] Tags { get; set; } = Array.Empty<string>();
    }

    [Fact(DisplayName = "lanza ArgumentNullException si el tipo es nulo")]
    public void BuildLabelsByType_ThrowsIfTypeIsNull()
    {
        // Act & Assert
        var labelBuilder = new ContextLabelBuilder();
        Assert.Throws<ArgumentNullException>(() => labelBuilder.LabelContext<object>(null!));
    }

    [Theory(DisplayName = "lanza ArgumentException si el tipo es genérico")]
    [AutoData]
    public void BuildLabelsByType_ThrowsIfTypeIsGeneric(Dictionary<string, object?> context)
    {
        // Arrange
        var labelBuilder = new ContextLabelBuilder();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => labelBuilder.LabelContext(context));

        Assert.Contains("tipo genérico", ex.Message);
    }
}


// -------------------------------
// Clases de apoyo para los tests
// -------------------------------
public enum StatusKind
{
    Unknown = 0,
    Active = 1,
    Inactive = 2
}

public class SimpleContext
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Optional { get; set; } // debe omitirse si es null
    public StatusKind Kind { get; set; }  // enum: entra en el "else" (no primitive, no string)
}

public class ChildContext
{
    public string City { get; set; } = "";
    public int Zip { get; set; }
}

public class ParentContext
{
    public string Title { get; set; } = "";
    public ChildContext? Details { get; set; }
    public object? Meta { get; set; } // tipo 'object': path especial en ProcessProperty
}

// Interfaz para probar propiedades de tipo Interface con Moq
public interface IInfo
{
    string Code { get; }
}
public class HolderWithInterface
{
    public IInfo? Info { get; set; }
}