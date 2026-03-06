namespace Observability.Tests.Implementation.Traces;
using AutoFixture;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Observability.Abstractions;
using Xunit;

public class TracesServiceTests
{
    private readonly Fixture _fixture = new Fixture();

    [Fact]
    [DisplayName("Constructor debe inicializar ActivitySource y ServiceScopeFactory")]
    public void Constructor_ShouldInitializeDependencies()
    {
        // Arrange
        var activitySource = new ActivitySource("TestSource");
        var scopeFactoryMock = new Mock<IServiceScopeFactory>().Object;

        // Act
        var service = new TracesService(activitySource, scopeFactoryMock);

        // Assert
        Assert.NotNull(service);
        Assert.Same(activitySource, GetField<ActivitySource>(service, "_activitySource"));
        Assert.Same(scopeFactoryMock, GetField<IServiceScopeFactory>(service, "_serviceProvider"));
    }

    [Fact]
    [DisplayName("GetCurrentActivity debe devolver una nueva Activity")]
    public void GetCurrentActivity_ShouldReturnNewActivity()
    {
        // Arrange
        var activitySource = new ActivitySource("TestSource");

        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "TestSource",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        var scopeFactoryMock = new Mock<IServiceScopeFactory>().Object;
        var service = new TracesService(activitySource, scopeFactoryMock);

        var builder = CreateTraceBuilder("TestActivity");

        // Act
        var activity = service.GetCurrentActivity(builder);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal("TestActivity", activity.DisplayName);
    }

    [Fact]
    [DisplayName("RegisterActivity debe inyectar Activity y Service en ActivityProcess")]
    public void RegisterActivity_ShouldInjectActivityAndService()
    {
        // Arrange
        var activitySource = new ActivitySource("TestSource");

        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "TestSource",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        }; 
        ActivitySource.AddActivityListener(listener);

        var process = new ActivityProcess(new Mock<ILabelNameBuilder>().Object);

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider.GetService(typeof(ActivityProcess))).Returns(process);

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        var service = new TracesService(activitySource, scopeFactoryMock.Object);
        var builder = CreateTraceBuilder("RegisterActivity");

        // Act
        var result = service.RegisterActivity(builder);

        // Assert
        Assert.NotNull(result);
        Assert.Same(service, GetProperty<ITracesService>(result, "Service"));
        Assert.NotNull(GetProperty<Activity>(result, "Activity"));
    }

    [Fact]
    [DisplayName("RegisterActivity debe lanzar excepción si ActivityProcess no está registrado")]
    public void RegisterActivity_ShouldThrowIfActivityProcessNotRegistered()
    {
        // Arrange
        var activitySource = new ActivitySource("TestSource");
        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider.GetService(typeof(ActivityProcess))).Returns(null!);

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        var service = new TracesService(activitySource, scopeFactoryMock.Object);
        var builder = CreateTraceBuilder("InvalidActivity");

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => service.RegisterActivity(builder));
        Assert.Contains("No se pueden crear ActivityProcess", ex.Message);
    }

    [Fact]
    [DisplayName("NewActivity debe crear actividad con parentContext si existe")]
    public void NewActivity_ShouldCreateActivityWithParentContext()
    {
        // Arrange
        var activitySource = new ActivitySource("TestSource");

        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "TestSource",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);


        var service = new TracesService(activitySource, new Mock<IServiceScopeFactory>().Object);

        var builder = CreateTraceBuilder("ParentContextActivity");

        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom(); 
        InjectInternalProperty(builder, "PropagationContext", new PropagationContext(
            new ActivityContext(traceId, spanId, ActivityTraceFlags.Recorded), Baggage.Current));

        // Act
        var activity = InvokeInternalMethod<Activity>(service, "NewActivity", builder);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal("ParentContextActivity", activity.DisplayName);
    }

    [Fact]
    [DisplayName("NewActivity debe agregar evento si builder.Activity no es nulo")]
    public void NewActivity_ShouldAddEventIfBuilderActivityExists()
    {
        // Arrange
        var activitySource = new ActivitySource("TestSource");

        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "TestSource",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);


        var service = new TracesService(activitySource, new Mock<IServiceScopeFactory>().Object);

        var builder = CreateTraceBuilder("ChildActivity");
        var parentActivity = new Activity("ParentActivity");
        InjectInternalProperty(builder, "Activity", parentActivity);

        // Act
        var activity = InvokeInternalMethod<Activity>(service, "NewActivity", builder);

        // Assert
        Assert.NotNull(activity);
        Assert.Contains(parentActivity.Events, e => e.Name == "ChildActivity");
    }

    // Helpers
    private TraceBuilder CreateTraceBuilder(string name)
    {
        var instance = Activator.CreateInstance(typeof(TraceBuilder), name);
        InjectInternalProperty(instance!, "ActivityKind", ActivityKind.Internal);
        Assert.IsType<TraceBuilder>(instance);
        return (TraceBuilder)instance;
    }

    private void InjectInternalProperty(object target, string propertyName, object value)
    {
        var prop = target.GetType().GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (prop == null)
            throw new InvalidOperationException($"Property '{propertyName}' not found.");
        prop.SetValue(target, value);
    }

    private T? GetProperty<T>(object obj, string propertyName)
    {
        var prop = obj.GetType().GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        return (T?)prop?.GetValue(obj);
    }

    private T? GetField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        return (T?)field?.GetValue(obj);
    }

    private T? InvokeInternalMethod<T>(object target, string methodName, params object[] parameters)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        return (T?)method?.Invoke(target, parameters);
    }
}
