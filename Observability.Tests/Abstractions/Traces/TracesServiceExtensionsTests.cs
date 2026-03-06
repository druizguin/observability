namespace Observability.Tests.Abstractions.Traces;

using System.ComponentModel;
using AutoFixture;
using Moq;
using Xunit;
using Observability.Abstractions;
using System.Diagnostics;
using System;
using System.Reflection;

public class TracesServiceExtensionsTests
{
    private readonly Fixture _fixture = new Fixture();

    [Fact]
    [DisplayName("Configure debe devolver TraceBuilder con propiedades correctas")]
    public void Configure_ShouldReturnTraceBuilderWithCorrectProperties()
    {
        // Arrange
        var serviceMock = new Mock<ITracesService>();
        var name = _fixture.Create<string>();
        var activityKind = ActivityKind.Client;

        // Act
        var result = TracesBuilderExtensions.Configure(serviceMock.Object, name, activityKind);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(name, GetProperty<string>(result, "Name"));
        Assert.Equal(activityKind, GetProperty<ActivityKind>(result, "ActivityKind"));
        Assert.Same(serviceMock.Object, GetProperty<ITracesService>(result, "Traces"));
    }

    [Fact]
    [DisplayName("Configure debe lanzar ArgumentNullException si service es null")]
    public void Configure_ShouldThrowIfServiceIsNull()
    {
        // Arrange
        ITracesService service = null!;
        var name = _fixture.Create<string>();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => TracesBuilderExtensions.Configure(service, name));
        Assert.Equal("service", ex.ParamName);
    }

    [Fact]
    [DisplayName("Configure debe lanzar ArgumentException si name es vacío")]
    public void Configure_ShouldThrowIfNameIsEmpty()
    {
        // Arrange
        var serviceMock = new Mock<ITracesService>();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => TracesBuilderExtensions.Configure(serviceMock.Object, ""));
        Assert.Equal("name", ex.ParamName);
    }

    [Fact]
    [DisplayName("Activity debe crear TraceBuilder y llamar a RegisterActivity")]
    public void Activity_ShouldCreateTraceBuilderAndCallRegisterActivity()
    {
        // Arrange
        var serviceMock = new Mock<ITracesService>();
        var processMock = new Mock<IActivityProcess>().Object;
        serviceMock.Setup(s => s.RegisterActivity(It.IsAny<TraceBuilder>())).Returns(processMock);

        var name = _fixture.Create<string>();

        // Act
        var result = TracesBuilderExtensions.Activity(serviceMock.Object, name);

        // Assert
        Assert.Same(processMock, result);
        serviceMock.Verify(s => s.RegisterActivity(It.IsAny<TraceBuilder>()), Times.Once);
    }

    [Fact]
    [DisplayName("ChildActivity debe crear TraceBuilder con Activity y llamar a RegisterActivity")]
    public void ChildActivity_ShouldCreateTraceBuilderAndCallRegisterActivity()
    {
        // Arrange
        var serviceMock = new Mock<ITracesService>();
        var processMock = new Mock<IActivityProcess>();
        processMock.SetupGet(p => p.Service).Returns(serviceMock.Object);
        processMock.SetupGet(p => p.Activity).Returns(new Activity("ParentActivity"));

        var expectedProcess = new Mock<IActivityProcess>().Object;
        serviceMock.Setup(s => s.RegisterActivity(It.IsAny<TraceBuilder>())).Returns(expectedProcess);

        var name = _fixture.Create<string>();

        // Act
        var result = TracesBuilderExtensions.ChildActivity(processMock.Object, name);

        // Assert
        Assert.Same(expectedProcess, result);
        serviceMock.Verify(s => s.RegisterActivity(It.IsAny<TraceBuilder>()), Times.Once);
    }

    [Fact]
    [DisplayName("CreateChildActivity debe devolver TraceBuilder con propiedades correctas")]
    public void CreateChildActivity_ShouldReturnTraceBuilderWithCorrectProperties()
    {
        // Arrange
        var serviceMock = new Mock<ITracesService>();
        var processMock = new Mock<IActivityProcess>();
        processMock.SetupGet(p => p.Service).Returns(serviceMock.Object);

        var name = _fixture.Create<string>();
        var activityKind = ActivityKind.Server;

        // Act
        var result = TracesBuilderExtensions.CreateChildActivity(processMock.Object, name, activityKind);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(name, GetProperty<string>(result, "Name"));
        Assert.Equal(activityKind, GetProperty<ActivityKind>(result, "ActivityKind"));
        Assert.Same(serviceMock.Object, GetProperty<ITracesService>(result, "Traces"));
    }

    [Fact]
    [DisplayName("AsType debe cambiar ActivityKind y devolver la misma instancia")]
    public void AsType_ShouldChangeActivityKindAndReturnSameInstance()
    {
        // Arrange
        var builder = CreateTraceBuilder("TestTrace", ActivityKind.Internal);
        var newKind = ActivityKind.Client;

        // Act
        var result = TracesBuilderExtensions.AsType(builder, newKind);

        // Assert
        Assert.Equal(newKind, GetProperty<ActivityKind>(builder, "ActivityKind"));
        Assert.Same(builder, result);
    }

    [Fact]
    [DisplayName("Build debe llamar a RegisterActivity en Traces")]
    public void Build_ShouldCallRegisterActivityOnTraces()
    {
        // Arrange
        var serviceMock = new Mock<ITracesService>();
        var expectedProcess = new Mock<IActivityProcess>().Object;
        serviceMock.Setup(s => s.RegisterActivity(It.IsAny<TraceBuilder>())).Returns(expectedProcess);

        var builder = CreateTraceBuilder("TraceBuild", ActivityKind.Internal);
        SetProperty(builder, "Traces", serviceMock.Object);

        // Act
        var result = TracesBuilderExtensions.Build(builder);

        // Assert
        Assert.Same(expectedProcess, result);
        serviceMock.Verify(s => s.RegisterActivity(builder), Times.Once);
    }

    // Helpers for reflection
    private static T? GetProperty<T>(object obj, string propertyName)
    {
        var prop = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        return (T?)prop?.GetValue(obj);
    }

    private static void SetProperty(object obj, string propertyName, object value)
    {
        var prop = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(prop);
        prop.SetValue(obj, value);
    }

    private TraceBuilder CreateTraceBuilder(string name, ActivityKind kind)
    {
        var type = typeof(TraceBuilder) ?? throw new InvalidOperationException("TraceBuilder type not found");
        var instance = Activator.CreateInstance(type, name);
        Assert.NotNull(instance);
        SetProperty(instance, "ActivityKind", kind);
        Assert.IsType<TraceBuilder>(instance);    
        return (TraceBuilder)instance;
    }
}
