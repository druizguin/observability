namespace Observability.Tests.Abstractions.Observability;


using AutoFixture;
using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Observability.Abstractions;
using Xunit;

public class SimpleCtx
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

// Spy para contar constructor de LabelBuilder (para validar Lazy bajo concurrencia)
public class SpyLabelBuilder : ContextLabelBuilder
{
    public static int ConstructorCount;

    public SpyLabelBuilder()
    {
        Interlocked.Increment(ref ConstructorCount);
    }
}


public class LabelsBag : IDictionary<string, object?>
{
    private readonly Dictionary<string, object?> _inner = new(StringComparer.Ordinal);

    // Soporte para AddRange(params tuples)
    public void AddRange(params (string key, object? value)[] parameters)
    {
        foreach (var (k, v) in parameters)
            _inner[k] = v;
    }

    // Soporte para AddRange(diccionario)
    public void AddRange(IDictionary<string, object?> dic)
    {
        foreach (var kvp in dic)
            _inner[kvp.Key] = kvp.Value;
    }

    // Exposición para assertions
    public IReadOnlyDictionary<string, object?> AsReadOnly() => _inner;

    // ==== Implementación mínima de IDictionary para permitir indexación ====
    public object? this[string key]
    {
        get => _inner.TryGetValue(key, out var v) ? v : null;
        set => _inner[key] = value;
    }

    public ICollection<string> Keys => _inner.Keys;
    public ICollection<object?> Values => _inner.Values;
    public int Count => _inner.Count;
    public bool IsReadOnly => false;

    public void Add(string key, object? value) => _inner.Add(key, value);
    public void Add(KeyValuePair<string, object?> item) => _inner.Add(item.Key, item.Value);
    public void Clear() => _inner.Clear();
    public bool Contains(KeyValuePair<string, object?> item) => _inner.ContainsKey(item.Key) && Equals(_inner[item.Key], item.Value);
    public bool ContainsKey(string key) => _inner.ContainsKey(key);
    public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
    {
        foreach (var kv in _inner)
            array[arrayIndex++] = kv;
    }
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _inner.GetEnumerator();
    public bool Remove(string key) => _inner.Remove(key);
    public bool Remove(KeyValuePair<string, object?> item) => _inner.Remove(item.Key);
    public bool TryGetValue(string key, out object? value) => _inner.TryGetValue(key, out value);
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _inner.GetEnumerator();
}

/// <summary>
/// Implementación mínima para pruebas del contrato de IObservabilityLabels.
/// </summary>
public class ObservabilityLabelsStub : IObservabilityLabels
{
    public ObservabilityLabelsStub(LabelsBag bag = null!)
    {
        Labels = bag ?? new LabelsBag();
    }

    public IDictionary<string, object?> Labels { get; set; }
}

public class ObservabilityLabelsExtensionsTests
{
    private readonly Fixture _fx = new();
    private static FieldInfo GetLazyField()
    {
        var type = typeof(ObservabilityLabelsExtensions);
        var field = type.GetField("labelBuilderLazy", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(field);
        return field!;
    }

    private static PropertyInfo GetLabelBuilderProperty()
    {
        var type = typeof(ObservabilityLabelsExtensions);
        var prop = type.GetProperty("LabelBuilder", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(prop);
        return prop!;
    }

    [Fact]
    [DisplayName("La propiedad privada LabelBuilder devuelve el mismo LabelBuilder que Lazy.Value")]
    public void Private_LabelBuilder_Property_Returns_Lazy_Value()
    {
        // Arrange
        var lazyField = GetLazyField();
        var prop = GetLabelBuilderProperty();

        // Preparamos un Lazy nuevo para aislar la prueba
        var replacement = new Lazy<ContextLabelBuilder>(() => new ContextLabelBuilder(), LazyThreadSafetyMode.ExecutionAndPublication);
        lazyField.SetValue(null, replacement);

        // Act
        var viaProperty = prop.GetValue(null);
        var viaLazy = ((Lazy<ContextLabelBuilder>)lazyField.GetValue(null)!).Value;

        // Assert
        Assert.NotNull(viaProperty);
        Assert.Same(viaLazy, viaProperty);
    }

    [Fact]
    [DisplayName("Lazy<LabelBuilder> se inicializa una sola vez aunque haya concurrencia")]
    public void Private_Lazy_LabelBuilder_Is_Initialized_Once_Under_Concurrency()
    {
        // Arrange
        SpyLabelBuilder.ConstructorCount = 0;
        var lazyField = GetLazyField();

        // Sustituimos el Lazy por uno que cree SpyLabelBuilder para poder contar construcciones
        var replacement = new Lazy<ContextLabelBuilder>(() => new SpyLabelBuilder(), LazyThreadSafetyMode.ExecutionAndPublication);
        lazyField.SetValue(null, replacement);

        // Disparamos múltiples accesos concurrentes a la propiedad (que fuerza Lazy.Value)
        var prop = GetLabelBuilderProperty();

        const int parallelism = 32;
        Parallel.For(0, parallelism, _ =>
        {
            var instance = (ContextLabelBuilder)prop.GetValue(null)!;
            // Usamos el instance para algo trivial que no falle
            var dict = instance.LabelContext(new SimpleCtx { Id = 1, Name = "x" });
            Assert.True(dict.Count >= 1);
        });

        // Assert
        Assert.Equal(1, SpyLabelBuilder.ConstructorCount);
    }

    [Fact]
    [DisplayName("Se puede reinyectar el Lazy privado con reflection y la instancia cambia")]
    public void Private_Lazy_Can_Be_Replaced_Via_Reflection_Instance_Changes()
    {
        // Arrange
        var lazyField = GetLazyField();
        var prop = GetLabelBuilderProperty();

        // Lazy #1
        var lazy1 = new Lazy<ContextLabelBuilder>(() => new ContextLabelBuilder(), LazyThreadSafetyMode.ExecutionAndPublication);
        lazyField.SetValue(null, lazy1);
        var inst1 = (ContextLabelBuilder)prop.GetValue(null)!;

        // Lazy #2 (nueva instancia)
        var lazy2 = new Lazy<ContextLabelBuilder>(() => new ContextLabelBuilder(), LazyThreadSafetyMode.ExecutionAndPublication);
        lazyField.SetValue(null, lazy2);
        var inst2 = (ContextLabelBuilder)prop.GetValue(null)!;

        // Assert
        Assert.NotSame(inst1, inst2);
    }

    [Fact]
    [DisplayName("LabelContext en la extensión usa el LabelBuilder y aplica doble prefijo (comportamiento actual)")]
    public void Extension_LabelContext_Uses_LabelBuilder_And_Double_Prefix_Current_Behavior()
    {
        // Arrange
        var tags = new ObservabilityLabelsStub(new LabelsBag());

        var ctx = new SimpleCtx { Id = 7, Name = "David" };
        var prefix = "req.";

        // Act
        ObservabilityLabelsExtensions.LabelContext(tags, ctx, prefix);

        // Assert
        // LabelBuilder.LabelContext ya aplica "req." -> "req.id" y "req.name"
        // La extensión vuelve a prefijar: "req." + "req.id" => "req.req.id"
        Assert.Equal("7", tags.Labels["req.id"]);
        Assert.Equal("David", tags.Labels["req.name"]);
    }

    [Fact]
    [DisplayName("Extension LabelContext normaliza prefijo sin punto final y resulta en doble prefijo")]
    public void Extension_LabelContext_Normalizes_Prefix_And_Doubles()
    {
        // Arrange
        var labels = new ObservabilityLabelsStub();
        var tags = new Mock<IObservabilityLabels>(MockBehavior.Strict);
        var ctx = new SimpleCtx { Id = 1, Name = "A" };

        // Act
        labels.LabelContext(ctx, "env"); // normaliza a "env."

        // Assert
        Assert.Equal("1", labels.Labels["env.id"]);
        Assert.Equal("A", labels.Labels["env.name"]);
    }

    [Fact]
    [DisplayName("Extension LabelContext traduce excepciones internas a InvalidCastException")]
    public void Extension_LabelContext_Maps_Exception_To_InvalidCastException()
    {
        // Arrange
        var tags = new ObservabilityLabelsStub(new LabelsBag());

        // Act + Assert
        // Pasar context = null -> LabelBuilder.LabelContext<T> lanza ArgumentNullException -> catch -> InvalidCastException
        var ex = Assert.Throws<InvalidCastException>(() =>
            ObservabilityLabelsExtensions.LabelContext(tags, (SimpleCtx)null!, "req."));
        Assert.Contains("No se pudo serializar el contexto", ex.Message);
    }
    [Fact]
    [DisplayName("Label(key, value) agrega o actualiza una etiqueta y devuelve la misma instancia")]
    public void Label_AddsOrUpdates_SingleTag_ReturnsSameInstance()
    {
        // Arrange
        var tags = new ObservabilityLabelsStub();
        var key = _fx.Create<string>();
        var value = _fx.Create<object>();

        // Act
        var returned = ObservabilityLabelsExtensions.Label(tags, key, value);

        // Assert
        Assert.Same(tags, returned);
        Assert.True(tags.Labels.ContainsKey(key));
        Assert.Equal(value, tags.Labels[key]);
    }

    [Fact]
    [DisplayName("Label(params tuples) agrega múltiples etiquetas y preserva últimas escrituras")]
    public void Label_Adds_MultipleTuples()
    {
        // Arrange
        var tags = new ObservabilityLabelsStub();
        var k1 = "env";
        var k2 = "region";

        // Act
        ObservabilityLabelsExtensions.Label(tags,
            (k1, "prod"),
            (k2, "eu"),
            (k1, "staging") // debe sobreescribir env
        );

        // Assert
        Assert.Equal("staging", tags.Labels[k1]); // última escritura prevalece
        Assert.Equal("eu", tags.Labels[k2]);
        Assert.Equal(2, tags.Labels.Count);
    }

    [Fact]
    [DisplayName("LabelFromDictionary agrega todas las entradas del diccionario al contenedor")]
    public void LabelFromDictionary_Adds_All_Dictionary_Entries()
    {
        // Arrange
        var tags = new ObservabilityLabelsStub();
        var dic = new Dictionary<string, object?>
        {
            ["user.id"] = 123,
            ["user.country"] = "ES",
            ["empty"] = null
        };

        // Act
        var returned = ObservabilityLabelsExtensions.LabelFromDictionary(tags, dic);

        // Assert
        Assert.Same(tags, returned);
        Assert.Equal(3, tags.Labels.Count);
        Assert.Equal(123, tags.Labels["user.id"]);
        Assert.Equal("ES", tags.Labels["user.country"]);
        Assert.Null(tags.Labels["empty"]);
    }

    [Fact]
    [DisplayName("Label(params) acepta valores null y los guarda tal cual")]
    public void Label_Accepts_Null_Values()
    {
        // Arrange
        var tags = new ObservabilityLabelsStub();
        var k = "nullable";

        // Act
        ObservabilityLabelsExtensions.Label(tags, (k, null));

        // Assert
        Assert.True(tags.Labels.ContainsKey(k));
        Assert.Null(tags.Labels[k]);
    }
}
