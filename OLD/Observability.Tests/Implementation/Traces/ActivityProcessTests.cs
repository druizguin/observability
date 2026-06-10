namespace Observability.Tests.Implementation.Traces;
using System;
using System.ComponentModel;
using Xunit;
using System.Diagnostics;
using System.Threading.Tasks;
using AutoFixture;
using Moq;
using Observability.Abstractions;
using System.Reflection;

public class ActivityProcessTests
{
    private readonly Fixture _fixture = new Fixture();

    [Fact]
    [DisplayName("Debe capturar el error ")]
    public void ShouldCaptureError()
    {
        // Arrange
        var tracesNameBuilderMock = new Mock<ILabelNameBuilder>().Object;
        var process = new ActivityProcess(tracesNameBuilderMock);

        var activity = new Activity("InjectedActivity");
        InjectInternalProperty(process, "Activity", activity);

        var expected = _fixture.Create<int>();

        // Act
        Assert.Throws<InvalidOperationException>(() =>
            process.Execute(p=> throw new InvalidOperationException("Test exception")));

        // Assert
        Assert.NotNull(process.Activity);
        Assert.Equal(ActivityStatusCode.Error, process.Activity.Status);
    }


    [Fact]
    [DisplayName("Debe inyectar Activity y ejecutar método correctamente")]
    public void ShouldInjectActivityAndExecute()
    {
        // Arrange
        var tracesNameBuilderMock = new Mock<ILabelNameBuilder>().Object;
        var process = new ActivityProcess(tracesNameBuilderMock);

        var activity = new Activity("InjectedAsyncActivity");
        InjectInternalProperty(process, "Activity", activity);

        // Act
        process.Execute(p => Thread.Sleep(1));

        // Assert
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);
    }

    [Fact]
    [DisplayName("Debe inyectar Activity por reflexión y ejecutar correctamente")]
    public void ShouldInjectActivityViaReflectionAndExecute()
    {
        // Arrange
        var tracesNameBuilderMock = new Mock<ILabelNameBuilder>().Object;
        var process = new ActivityProcess(tracesNameBuilderMock);

        var activity = new Activity("InjectedActivity");
        InjectInternalProperty(process, "Activity", activity);

        var expected = _fixture.Create<int>();

        // Act
        var result = process.Execute(p => expected);

        // Assert
        Assert.Equal(expected, result);
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);
    }

    [Fact]
    [DisplayName("Debe inyectar Activity y manejar excepción en Execute")]
    public void ShouldInjectActivityAndHandleException()
    {
        // Arrange
        var tracesNameBuilderMock = new Mock<ILabelNameBuilder>().Object;
        var process = new ActivityProcess(tracesNameBuilderMock);

        var activity = new Activity("InjectedActivity");
        InjectInternalProperty(process, "Activity", activity);

        bool onErrorCalled = false;

        // Act
        var result = process.Execute<int>(
            p => throw new InvalidOperationException("Error"),
            (p, ex) =>
            {
                onErrorCalled = true;
                return 999;
            });

        // Assert
        Assert.True(onErrorCalled);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
    }

    [Fact]
    [DisplayName("Debe inyectar Activity y ejecutar método asíncrono correctamente")]
    public async Task ShouldInjectActivityAndExecuteAsync()
    {
        // Arrange
        var tracesNameBuilderMock = new Mock<ILabelNameBuilder>().Object;
        var process = new ActivityProcess(tracesNameBuilderMock);

        var activity = new Activity("InjectedAsyncActivity");
        InjectInternalProperty(process, "Activity", activity);

        var expected = "AsyncResult";

        // Act
        var result = await process.ExecuteAsync(p => Task.FromResult(expected));

        // Assert
        Assert.Equal(expected, result);
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);
    }

    [Fact]
    [DisplayName("Dispose debe detener y liberar Activity inyectada")]
    public void Dispose_ShouldStopAndDisposeInjectedActivity()
    {
        // Arrange
        var tracesNameBuilderMock = new Mock<ILabelNameBuilder>().Object;
        var process = new ActivityProcess(tracesNameBuilderMock);

        var activity = new Activity("DisposeActivity");
        activity.Start();
        InjectInternalProperty(process, "Activity", activity);

        // Act
        process.Dispose();

        // Assert
        Assert.True(activity.IsStopped);
    }

    // Helper para inyectar propiedad internal
    private static void InjectInternalProperty(object target, string propertyName, object value)
    {
        var prop = target.GetType().GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (prop == null)
            throw new InvalidOperationException($"Property '{propertyName}' not found.");
        prop.SetValue(target, value);
    }
}
