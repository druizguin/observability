namespace Observability.Tests.Abstractions.Traces;

using System.ComponentModel;
using AutoFixture;
using Moq;
using Xunit;
using Observability.Abstractions;
using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.Context.Propagation;

public class TraceBuilderTests
{
    private readonly Fixture _fixture = new Fixture();

    [Fact]
    [DisplayName("Debe inicializar ActivityKind como Internal por defecto")]
    public void ShouldInitializeActivityKindAsInternal()
    {
        // Act
        var builder = new TraceBuilder("name");

        // Assert
        Assert.Equal(ActivityKind.Internal, builder.ActivityKind);
    }

    [Fact]
    [DisplayName("Debe inicializar Context como diccionario vacío")]
    public void ShouldInitializeContextAsEmptyDictionary()
    {
        // Act
        var builder = new TraceBuilder("name");

        // Assert
        Assert.NotNull(builder.Context);
        Assert.Empty(builder.Context);
    }

    [Fact]
    [DisplayName("Debe permitir asignar y leer propiedades públicas")]
    public void ShouldSetAndGetPublicProperties()
    {
        // Arrange
        var builder = new TraceBuilder("name");
        var name = _fixture.Create<string>();
        var activity = new Activity("TestActivity");
        var propagationContext = new PropagationContext();

        // Act
        builder.Name = name;
        builder.Activity = activity;
        builder.PropagationContext = propagationContext;

        // Assert
        Assert.Equal(name, builder.Name);
        Assert.Same(activity, builder.Activity);
        Assert.Equal(propagationContext, builder.PropagationContext);
    }

    [Fact]
    [DisplayName("ToString debe devolver el valor de Name")]
    public void ToString_ShouldReturnName()
    {
        // Arrange
        var name = _fixture.Create<string>();
        var builder = new TraceBuilder(name);

        // Act
        var result = builder.ToString();

        // Assert
        Assert.Equal(name, result);
    }

    [Fact]
    [DisplayName("Debe permitir asignar Traces mediante reflexión")]
    public void ShouldAllowSettingTracesViaReflection()
    {
        // Arrange
        var builder = new TraceBuilder("name");
        var mockTraces = new Mock<ITracesService>().Object;
        var property = typeof(TraceBuilder).GetProperty("Traces", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(property);

        // Act
        property.SetValue(builder, mockTraces);
        var value = property.GetValue(builder);

        // Assert
        Assert.NotNull(value);
        Assert.Same(mockTraces, value);
    }

    [Fact]
    [DisplayName("Debe permitir agregar elementos al Context")]
    public void ShouldAllowAddingItemsToContext()
    {
        // Arrange
        var builder = new TraceBuilder("name");
        var key = _fixture.Create<string>();
        var value = _fixture.Create<string>();

        // Act
        builder.Context[key] = value;

        // Assert
        Assert.True(builder.Context.ContainsKey(key));
        Assert.Equal(value, builder.Context[key]);
    }

    [Fact]
    [DisplayName("Debe manejar valores nulos en Name sin lanzar excepción")]
    public void ShouldHandleNullNameInToString()
    {
        // Arrange
        Assert.Throws<ArgumentNullException>(() => new TraceBuilder(null!));
        Assert.Throws<ArgumentException>(() => new TraceBuilder(""));
    }
}
