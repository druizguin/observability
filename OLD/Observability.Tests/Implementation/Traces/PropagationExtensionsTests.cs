namespace Observability.Tests.Implementation.Traces;

using AutoFixture;
using Microsoft.AspNetCore.Http;
using Moq;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Observability.Abstractions;
using Xunit;

public class PropagationExtensionsTests
{
    private readonly Fixture _fixture = new Fixture();

    [Fact]
    [DisplayName("CorrelateFromRabbit debe lanzar ArgumentNullException si headers es null")]
    public void CorrelateFromRabbit_ShouldThrowIfHeadersIsNull()
    {
        // Arrange
        var builder = CreateTraceBuilder();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => PropagationExtensions.CorrelateFromRabbit(builder!, null));
        Assert.Equal("headers", ex.ParamName);
    }

    [Fact]
    [DisplayName("CorrelateFromRabbit debe asignar PropagationContext al TraceBuilder")]
    public void CorrelateFromRabbit_ShouldAssignPropagationContext()
    {
        // Arrange
        var builder = CreateTraceBuilder();
        var headers = new Dictionary<string, object?>
        {
            { "traceparent", Encoding.UTF8.GetBytes("00-abc123-def456-01") }
        };

        // Act
        var result = PropagationExtensions.CorrelateFromRabbit(builder!, headers);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(GetProperty(result, "PropagationContext"));
    }

    [Fact]
    [DisplayName("CorrelateFrom debe devolver el mismo TraceBuilder si no hay cabecera x-traceid")]
    public void CorrelateFrom_ShouldReturnSameBuilderIfNoHeader()
    {
        // Arrange
        var builder = CreateTraceBuilder();
        var request = new DefaultHttpContext().Request;
       
        // Act
        var result = PropagationExtensions.CorrelateFrom(builder!, request);

        // Assert
        Assert.Same(builder, result);
        Assert.Null(GetProperty(result, "PropagationContext"));
    }

    [Fact]
    [DisplayName("CorrelateFrom debe asignar PropagationContext cuando la cabecera existe")]
    public void CorrelateFrom_ShouldAssignPropagationContextIfHeaderExists()
    {
        // Arrange
        var builder = CreateTraceBuilder();
        var request = new DefaultHttpContext().Request;
        var headersDict = new Dictionary<string, string?> { { "traceparent", "00-abc123-def456-01" } };
        request.Headers["x-traceid"] = JsonSerializer.Serialize(headersDict);

        // Act
        var result = PropagationExtensions.CorrelateFrom(builder!, request);

        // Assert
        Assert.NotNull(GetProperty(result, "PropagationContext"));
    }

    [Fact]
    [DisplayName("CorrelateTo debe agregar cabecera x-traceid al HttpClient")]
    public void CorrelateTo_ShouldAddTraceIdHeaderToHttpClient()
    {
        // Arrange
        var activityProcessMock = new Mock<IActivityProcess>();
        activityProcessMock.SetupGet(p => p.Activity).Returns(new Activity("TestActivity"));
        var client = new HttpClient();

        // Act
        PropagationExtensions.CorrelateTo(activityProcessMock.Object, client);

        // Assert
        Assert.True(client.DefaultRequestHeaders.Contains("x-traceid"));
    }

    //[Fact]
    //[DisplayName("GetPropagationHeaders debe devolver diccionario con claves propagadas")]
    //public void GetPropagationHeaders_ShouldReturnHeadersDictionary()
    //{
    //    // Arrange
    //    var activity = new Activity("TestActivity");
    //    activity.Start();
    //    var activitychild = new Activity("ChildActivity").SetParentId(activity.Id);
    //    activitychild.Start();
    //    // Act
    //    var headers = PropagationExtensions.GetPropagationHeaders(activitychild);

    //    // Assert
    //    Assert.NotNull(headers);
    //    Assert.NotEmpty(headers);
    //}

    // Helpers
    private static TraceBuilder? CreateTraceBuilder()
    {
        var type = typeof(TraceBuilder) ?? throw new InvalidOperationException("TraceBuilder type not found");
        return Activator.CreateInstance(type, "name") as TraceBuilder;
    }

    private static PropagationContext? GetProperty(TraceBuilder obj, string propertyName)
    {
        var prop = obj.GetType().GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(prop);
        return (PropagationContext?)prop.GetValue(obj);
    }
}
