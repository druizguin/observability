namespace Observability.Tests.Abstractions.Observability;


using AutoFixture;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Xunit;

// ============================
// TESTS
// ============================

public class ObservabilityBuilderExtensions_PrivateAndPublic_Tests
{
    private readonly Fixture _fx = new();

    private static MethodInfo GetPrivate_GetOtelEndpoint()
    {
        var type = typeof(ObservabilityBuilderExtensions);
        var mi = type.GetMethod("GetOtelEndpoint", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(mi);
        return mi!;
    }

    private static IHostApplicationBuilder MakeHostBuilder(string environment = "Tests", IEnumerable<KeyValuePair<string, string?>>? configPairs = null)
    {
        var builder = new Microsoft.Extensions.Hosting.HostApplicationBuilder(new Microsoft.Extensions.Hosting.HostApplicationBuilderSettings
        {
            EnvironmentName = environment
        });

        if (configPairs != null)
            builder.Configuration.AddInMemoryCollection(configPairs);

        return builder;
    }

    // -----------------------------
    // GetOtelEndpoint (privado) por reflexión
    // -----------------------------

    [Fact]
    [DisplayName("GetOtelEndpoint lanza ArgumentException si no hay OpentelemetryUrl ni en builder ni en options")]
    public void GetOtelEndpoint_Throws_When_Missing_Url()
    {
        // Arrange
        var hostBuilder = MakeHostBuilder();
        var obsBuilder = new ObservabilityBuilder(hostBuilder)
        {
            Options = new ObservabilityOptions { OpentelemetryUrl = null }
        };

        var mi = GetPrivate_GetOtelEndpoint();

        // Act + Assert
        var ex = Assert.Throws<TargetInvocationException>(() => mi.Invoke(null, new object[] { obsBuilder }));
        Assert.IsType<ArgumentNullException>(ex.InnerException);
        Assert.Contains("OpentelemetryUrl is not configured", ex.InnerException!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [DisplayName("GetOtelEndpoint usa obsBuilder.OpentelemetryUrl si está establecida y retorna Uri absoluto")]
    public void GetOtelEndpoint_Uses_ObsBuilder_Property_And_Returns_Uri()
    {
        // Arrange
        var hostBuilder = MakeHostBuilder();
        var obsBuilder = new ObservabilityBuilder(hostBuilder)
        {
            OpentelemetryUrl = "https://otel.example.com:4317",
            Options = new ObservabilityOptions { OpentelemetryUrl = "https://ignored.local" }
        };

        var mi = GetPrivate_GetOtelEndpoint();

        // Act
        var uri = (Uri)mi.Invoke(null, new object[] { obsBuilder })!;

        // Assert
        Assert.NotNull(uri);
        Assert.Equal("https://otel.example.com:4317/", uri.ToString());
        // No cambia el valor ya establecido
        Assert.Equal("https://otel.example.com:4317", obsBuilder.OpentelemetryUrl);
    }

    [Fact]
    [DisplayName("GetOtelEndpoint toma la URL desde Options si la propiedad de obsBuilder es null")]
    public void GetOtelEndpoint_FallsBack_To_Options_When_ObsBuilder_Property_Null()
    {
        // Arrange
        var hostBuilder = MakeHostBuilder();
        var url = "https://collector.mydomain.com:4317";
        var obsBuilder = new ObservabilityBuilder(hostBuilder)
        {
            OpentelemetryUrl = null,
            Options = new ObservabilityOptions { OpentelemetryUrl = url }
        };

        var mi = GetPrivate_GetOtelEndpoint();

        // Act
        var uri = (Uri)mi.Invoke(null, new object[] { obsBuilder })!;

        // Assert
        Assert.Equal(new Uri(url), uri);
        // Debe asignar obsBuilder.OpentelemetryUrl desde Options
        Assert.Equal(url, obsBuilder.OpentelemetryUrl);
    }

    [Fact]
    [DisplayName("GetOtelEndpoint lanza si la URL no es un URI absoluto válido")]
    public void GetOtelEndpoint_Throws_When_Invalid_Url()
    {
        // Arrange
        var hostBuilder = MakeHostBuilder();
        var obsBuilder = new ObservabilityBuilder(hostBuilder)
        {
            OpentelemetryUrl = "not-a-valid-url"
        };

        var mi = GetPrivate_GetOtelEndpoint();

        // Act + Assert
        var ex = Assert.Throws<TargetInvocationException>(() => mi.Invoke(null, new object[] { obsBuilder }));
        Assert.IsType<ArgumentNullException>(ex.InnerException);
        Assert.Contains("OpentelemetryUrl is not valid", ex.InnerException!.Message, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------
    // Configure (pública)
    // -----------------------------

    [Fact]
    [DisplayName("Configure aplica la función y establece Options en el builder")]
    public void Configure_Applies_Function_And_Sets_Options()
    {
        // Arrange
        var hostBuilder = MakeHostBuilder();
        var obsBuilder = new ObservabilityBuilder(hostBuilder);

        // Act
        ObservabilityBuilderExtensions.Configure(obsBuilder, opts =>
        {
            opts.EnableMetrics = true;
            opts.EnableTracing = true;
            opts.OpentelemetryUrl = "https://otel.local:4317";
            return opts;
        });

        // Assert
        Assert.NotNull(obsBuilder.Options);
        Assert.True(obsBuilder.Options!.EnableMetrics);
        Assert.True(obsBuilder.Options!.EnableTracing);
        Assert.Equal("https://otel.local:4317", obsBuilder.Options!.OpentelemetryUrl);
    }

    // -----------------------------
    // WithMetrics (pública)
    // -----------------------------

    [Fact]
    [DisplayName("WithMetrics lanza ArgumentNullException si la action es null")]
    public void WithMetrics_Throws_When_Action_Null()
    {
        // Arrange
        var hostBuilder = MakeHostBuilder();
        var obsBuilder = new ObservabilityBuilder(hostBuilder)
        {
            Options = ObservabilityOptions.Default()
        };

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() =>
            ObservabilityBuilderExtensions.WithMetrics(obsBuilder, action: null!));
    }

    [Fact]
    [DisplayName("WithMetrics lanza ArgumentNullException si obsBuilder es null")]
    public void WithMetrics_Throws_When_Builder_Null()
    {
        // Arrange
        ObservabilityBuilder? obsBuilder = null;

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() =>
            ObservabilityBuilderExtensions.WithMetrics(obsBuilder!, _ => { }));
    }

    [Fact]
    [DisplayName("WithMetrics lanza ArgumentNullException si Options es null en el builder")]
    public void WithMetrics_Throws_When_Options_Null()
    {
        // Arrange
        var hostBuilder = MakeHostBuilder();
        var obsBuilder = new ObservabilityBuilder(hostBuilder)
        {
            Options = null
        };

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() =>
            ObservabilityBuilderExtensions.WithMetrics(obsBuilder, _ => { }));
    }

    [Fact]
    [DisplayName("WithMetrics asigna la acción al Options.Metrics.MeterBuilderAction")]
    public void WithMetrics_Assigns_Action_To_Options()
    {
        // Arrange
        var hostBuilder = MakeHostBuilder();
        var options = ObservabilityOptions.Default();
        options.ApplicationCard = "arc.test.project.api";
        options.OpentelemetryUrl = "https://otel.test.local:4317";

        var obsBuilder = new ObservabilityBuilder(hostBuilder)
        {
            Options = options
        };
        obsBuilder.Configure(_ => options);

        bool invoked = false;

        // Act
        obsBuilder.WithMetrics(_ => invoked = true);

        // Assert
        Assert.NotNull(obsBuilder.Options!.Metrics.MeterBuilderAction);
        // Simulamos ejecución:
        obsBuilder.BuildObservability(); 

        Assert.True(invoked);
    }

    // -----------------------------
    // WithTraces (pública)
    // -----------------------------

    [Fact]
    [DisplayName("WithTraces lanza ArgumentNullException si la action es null")]
    public void WithTraces_Throws_When_Action_Null()
    {
        // Arrange
        var hostBuilder = MakeHostBuilder();
        var obsBuilder = new ObservabilityBuilder(hostBuilder)
        {
            Options = ObservabilityOptions.Default()
        };

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() =>
            ObservabilityBuilderExtensions.WithTraces(obsBuilder, action: null!));
    }

    [Fact]
    [DisplayName("WithTraces lanza ArgumentNullException si obsBuilder es null")]
    public void WithTraces_Throws_When_Builder_Null()
    {
        // Arrange
        ObservabilityBuilder? obsBuilder = null;

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() =>
            ObservabilityBuilderExtensions.WithTraces(obsBuilder!, _ => { }));
    }

    [Fact]
    [DisplayName("WithTraces lanza ArgumentNullException si Options es null")]
    public void WithTraces_Throws_When_Options_Null()
    {
        // Arrange
        var hostBuilder = MakeHostBuilder();
        var obsBuilder = new ObservabilityBuilder(hostBuilder)
        {
            Options = null
        };

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() =>
            ObservabilityBuilderExtensions.WithTraces(obsBuilder, _ => { }));
    }

    [Fact]
    [DisplayName("WithTraces asigna la acción al Options.Tracing.TracerBuilderAction")]
    public void WithTraces_Assigns_Action_To_Options()
    {
        // Arrange
        var hostBuilder = MakeHostBuilder();
        var options = ObservabilityOptions.Default();
        options.ApplicationCard = "arc.test.project.api";
        options.OpentelemetryUrl = "https://otel.test.local:4317";

        var obsBuilder = new ObservabilityBuilder(hostBuilder)
        {
            Options = options
        };
        obsBuilder.Configure(_ => options);

        bool invoked = false;

        // Act
        obsBuilder.WithTraces(_ => invoked = true);

        // Assert
        Assert.NotNull(obsBuilder.Options!.Tracing.TracerBuilderAction);
        // Simulamos ejecución:
        obsBuilder.BuildObservability();
        Assert.True(invoked);
    }
}

public class ObservabilityBuilderExtensionsTests
{
    private readonly Fixture _fx = new();

    private static IHostApplicationBuilder MakeHostBuilder(string environment = "DEV", IEnumerable<KeyValuePair<string, string?>>? configPairs = null)
    {
        var builder = new HostApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = environment
        });

        if (configPairs != null)
        {
            builder.Configuration.AddInMemoryCollection(configPairs);
        }

        return builder;
    }

    [Fact]
    [DisplayName("CreateObservabilityBuilder crea un ObservabilityBuilder con el mismo IHostApplicationBuilder")]
    public void CreateObservabilityBuilder_Creates_Wrapper()
    {
        // Arrange
        var hostBuilder = MakeHostBuilder();

        // Act
        var obsBuilder = ObservabilityBuilderExtensions.CreateObservabilityBuilder(hostBuilder);

        // Assert
        Assert.NotNull(obsBuilder);
        Assert.Same(hostBuilder, obsBuilder.Builder);
    }

    [Fact]
    [DisplayName("LoadFromConfiguration lanza si falta la sección Observability")]
    public void LoadFromConfiguration_Throws_When_Missing_Section()
    {
        // Arrange
        var hostBuilder = MakeHostBuilder();
        var obsBuilder = hostBuilder.CreateObservabilityBuilder();

        // Act
        var ex = Assert.Throws<ArgumentNullException>(() => obsBuilder.LoadFromConfiguration("Observability"));

        // Assert
        Assert.Contains("Observability section is missing in configuration settings", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [DisplayName("LoadFromConfiguration carga Options y AppCard (cuando ApplicationCard está configurado)")]
    public void LoadFromConfiguration_Loads_Options_And_AppCard()
    {
        // Arrange
        var key = "dev.arc.svc.api";
        var version = Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString();
        var config = new Dictionary<string, string?>
        {
            ["Observability:EnableMetrics"] = "false",
            ["Observability:EnableTracing"] = "true",
            ["Observability:OpentelemetryUrl"] = "https://otel.test.local:4317",
            // ApplicationCard (se mapeará a tu DTO que el ApplicationCard real acepta en su ctor)
            ["Observability:ApplicationCard"] = key,
        };

        var hostBuilder = MakeHostBuilder(configPairs: config);
        var obsBuilder = hostBuilder.CreateObservabilityBuilder();

        // Precondición
        Assert.Null(obsBuilder.Options);
        Assert.Null(obsBuilder.AppCard);

        // Act
        obsBuilder.LoadFromConfiguration(); // default section "Observability"
        obsBuilder.BuildObservability();

        // Assert
        Assert.NotNull(obsBuilder.Options);
        Assert.NotNull(obsBuilder.AppCard);
        Assert.Equal(key, obsBuilder.AppCard.Key);
        Assert.Equal(version, obsBuilder.AppCard.Version);
    }

    [Fact]
    [DisplayName("BuildObservability lanza cuando Options es null")]
    public void BuildObservability_Throws_When_Options_Is_Null()
    {
        // Arrange
        var hostBuilder = MakeHostBuilder();
        var obsBuilder = hostBuilder.CreateObservabilityBuilder();

        // Act
        var ex = Assert.Throws<ArgumentNullException>(() => obsBuilder.BuildObservability());

        // Assert
        Assert.Contains("Observability is not configured", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [DisplayName("BuildObservability retorna temprano cuando métricas y trazas están desactivadas")]
    public void BuildObservability_Returns_Early_When_Metrics_And_Tracing_Disabled()
    {
        // Arrange
        var config = new Dictionary<string, string?>
        {
            ["Observability:EnableMetrics"] = "false",
            ["Observability:EnableTracing"] = "false"
        };

        var hostBuilder = MakeHostBuilder(configPairs: config);
        var obsBuilder = hostBuilder.CreateObservabilityBuilder()
                                   .LoadFromConfiguration();

        // Act
        var returned = obsBuilder.BuildObservability();

        // Assert
        // Devuelve el mismo builder
        Assert.Same(hostBuilder, returned);

        // Debe haber configurado logging antes del return temprano
        var sp = hostBuilder.Services.BuildServiceProvider();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        Assert.NotNull(loggerFactory);

        // Debe haber dejado un logger en obsBuilder
        Assert.NotNull(obsBuilder.Logger);
    }

    [Fact]
    [DisplayName("BuildObservability lanza cuando EnableMetrics o EnableTracing está activo y AppCard es null")]
    public void BuildObservability_Throws_When_AppCard_Missing_And_Feature_Enabled()
    {
        // Arrange
        var config = new Dictionary<string, string?>
        {
            ["Observability:EnableMetrics"] = "true",  // activo (puede ser EnableTracing = false)
            ["Observability:EnableTracing"] = "false"
        };

        var hostBuilder = MakeHostBuilder(configPairs: config);
        var obsBuilder = hostBuilder.CreateObservabilityBuilder()
                                   .LoadFromConfiguration();

        // Sanity: AppCard no configurada en config
        Assert.Null(obsBuilder.AppCard);

        // Act
        var ex = Assert.Throws<ArgumentNullException>(() => obsBuilder.BuildObservability());

        // Assert
        Assert.Contains("Application Card is not configured", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
