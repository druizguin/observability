namespace Observability.Tests.Abstractions.Observability;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using AutoFixture;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;


// Simula el método usado en la extensión real
public static class ApplicationCardExtensions
{
    public static ApplicationCard BuildAppCard(IHostApplicationBuilder builder, string serviceName)
        => new ApplicationCard(serviceName);
}

public class ObservabilityBuilderSerilogExtensionsTests
{
    private readonly Fixture _fx = new();

    private static IHostApplicationBuilder MakeHostBuilder(IEnumerable<KeyValuePair<string, string?>> pairs)
    {
        var builder = new HostApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(pairs);
        return builder;
    }

    [Fact]
    [DisplayName("UseSerilog lee Serilog:WriteTo:OpenTelemetry y configura AppCard y OpentelemetryUrl")]
    public void UseSerilog_Configures_AppCard_And_Endpoint_From_Serilog_WriteTo_OpenTelemetry()
    {
        // Arrange
        var serviceName = "env.area.proyecto.app";
        var endpoint = "https://otel.myorg.com:4317";
        var config = new Dictionary<string, string?>
        {
            // Serilog:WriteTo array con un elemento OpenTelemetry
            ["Serilog:WriteTo:0:Name"] = "OpenTelemetry",
            ["Serilog:WriteTo:0:Args:ResourceAttributes:service.name"] = serviceName,
            ["Serilog:WriteTo:0:Args:Endpoint"] = endpoint
        };

        var hostBuilder = MakeHostBuilder(config);
        var obsBuilder = new ObservabilityBuilder(hostBuilder);

        // Act
        var returned = ObservabilityBuilderSerilogExtensions.UseSerilog(obsBuilder);

        // Assert
        Assert.Same(obsBuilder, returned);
        Assert.NotNull(obsBuilder.AppCard);
        Assert.Equal(serviceName, obsBuilder.AppCard!.Key);
        Assert.Equal(endpoint, obsBuilder.OpentelemetryUrl);
    }

    [Fact]
    [DisplayName("UseSerilog lanza ArgumentException si no hay elementos en Serilog:WriteTo")]
    public void UseSerilog_Throws_When_WriteTo_Missing()
    {
        // Arrange: no WriteTo
        var config = new Dictionary<string, string?>
        {
            ["Serilog:Using:0"] = "Serilog.Sinks.Console"
        };

        var hostBuilder = MakeHostBuilder(config);
        var obsBuilder = new ObservabilityBuilder(hostBuilder);
        
        // Act
        var ex = Assert.Throws<ArgumentException>(() =>
            ObservabilityBuilderSerilogExtensions.UseSerilog(obsBuilder));

        // Assert
        // En el throw final: new ArgumentException("observability serilog", "observability is not configured ...")
        Assert.Equal("Serilog:WriteTo:OpenTelemetry:Args:ResourceAttributes", ex.ParamName);
        Assert.Contains("observability is not configured", ex.Message);
    }

    [Fact]
    [DisplayName("UseSerilog lanza si un hijo de WriteTo no tiene Name")]
    public void UseSerilog_Throws_When_WriteTo_Child_Has_No_Name()
    {
        // Arrange: WriteTo con un elemento sin Name
        var config = new Dictionary<string, string?>
        {
            ["Serilog:WriteTo:0:Args:Some"] = "Value" // sin Name
        };
        var hostBuilder = MakeHostBuilder(config);
        var obsBuilder = new ObservabilityBuilder(hostBuilder);

        // Act
        var ex = Assert.Throws<ArgumentNullException>(() =>
            ObservabilityBuilderSerilogExtensions.UseSerilog(obsBuilder));

        // Assert
        // Lanza por ArgumentException.ThrowIfNullOrEmpty(name, $"{section.Key} Name is not present")
        Assert.Equal("0 Name is not present", ex.ParamName);
        // El mensaje que genera ThrowIfNullOrEmpty suele ser "Value cannot be null or empty."
        Assert.Contains("Value cannot be null", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [DisplayName("UseSerilog lanza si OpenTelemetry no tiene service.name")]
    public void UseSerilog_Throws_When_ServiceName_Missing()
    {
        // Arrange: OpenTelemetry presente pero sin service.name
        var config = new Dictionary<string, string?>
        {
            ["Serilog:WriteTo:0:Name"] = "OpenTelemetry",
            // Falta: Args:ResourceAttributes:service.name
            ["Serilog:WriteTo:0:Args:Endpoint"] = "https://otel-endpoint"
        };
        var hostBuilder = MakeHostBuilder(config);
        var obsBuilder = new ObservabilityBuilder(hostBuilder);

        // Act
        var ex = Assert.Throws<ArgumentNullException>(() =>
            ObservabilityBuilderSerilogExtensions.UseSerilog(obsBuilder));

        // Assert
        Assert.Equal("Serilog:WriteTo:OpenTelemetry:Args:ResourceAttributes:service.name", ex.ParamName);
        Assert.Contains("Value cannot be null", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [DisplayName("UseSerilog lanza si OpenTelemetry no tiene Endpoint")]
    public void UseSerilog_Throws_When_Endpoint_Missing()
    {
        // Arrange: OpenTelemetry sin Endpoint
        var config = new Dictionary<string, string?>
        {
            ["Serilog:WriteTo:0:Name"] = "OpenTelemetry",
            ["Serilog:WriteTo:0:Args:ResourceAttributes:service.name"] = "env.area.proyecto.app"
            // Falta: Args:Endpoint
        };
        var hostBuilder = MakeHostBuilder(config);
        var obsBuilder = new ObservabilityBuilder(hostBuilder);

        // Act
        var ex = Assert.Throws<ArgumentNullException>(() =>
            ObservabilityBuilderSerilogExtensions.UseSerilog(obsBuilder));

        // Assert
        Assert.Equal("Serilog:WriteTo:OpenTelemetry:Args:ResourceAttributes:Endpoint", ex.ParamName);
        Assert.Contains("Value cannot be null", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [DisplayName("UseSerilog reconoce 'OpenTelemetry' sin sensibilidad a mayúsculas/minúsculas")]
    public void UseSerilog_Matches_OpenTelemetry_Name_Case_Insensitive()
    {
        // Arrange: Name con casing mixto
        var serviceName = "env.area.proyecto.app";
        var endpoint = "http://collector:4317";
        var config = new Dictionary<string, string?>
        {
            ["Serilog:WriteTo:0:Name"] = "OpenTeleMeTry",
            ["Serilog:WriteTo:0:Args:ResourceAttributes:service.name"] = serviceName,
            ["Serilog:WriteTo:0:Args:Endpoint"] = endpoint
        };

        var hostBuilder = MakeHostBuilder(config);
        var obsBuilder = new ObservabilityBuilder(hostBuilder);

        // Act
        ObservabilityBuilderSerilogExtensions.UseSerilog(obsBuilder);

        // Assert
        Assert.NotNull(obsBuilder.AppCard);
        Assert.Equal(serviceName, obsBuilder.AppCard!.Key);
        Assert.Equal(endpoint, obsBuilder.OpentelemetryUrl);
    }
}
