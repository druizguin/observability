namespace Observability;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Observability.Abstractions;

/// <summary>
/// Extension helpers that wire up OpenTelemetry tracing and metrics, and register the observability services into DI.
/// </summary>
public static class ObservabilityBuilderExtensions
{
    /// <summary>
    /// Creates a new <see cref="ObservabilityBuilder"/> for the provided host builder.
    /// </summary>
    public static ObservabilityBuilder CreateObservabilityBuilder(this IHostApplicationBuilder builder)
    {
        var result = new ObservabilityBuilder(builder);
        return result;
    }

    /// <summary>
    /// Loads observability options from configuration into the builder.
    /// </summary>
    public static ObservabilityBuilder LoadFromConfiguration(
        this ObservabilityBuilder obsBuilder,
        string sectionName = "Observability")
    {
        var section = obsBuilder.Builder
            .Configuration
            .GetSection(sectionName);

        var config = section.Get<ObservabilityOptions>();

        if (config == null)
            throw new ArgumentNullException("Observability section is missing in configuration settings. (appsettings?)");

        obsBuilder.Options = config;       

        return obsBuilder;
    }

    /// <summary>
    /// Builds and registers the configured observability components (traces, metrics, observability services) into DI.
    /// </summary>
    public static IHostApplicationBuilder BuildObservability(this ObservabilityBuilder obsBuilder)
    {
        try
        {
            //Logs
            var builder = obsBuilder.Builder;

            builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
            builder.Services.AddLogging();

            using (var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
                       .AddConfiguration(obsBuilder.Builder.Configuration)
                       .AddConsole()
                       ))
            {
                obsBuilder.Logger = loggerFactory.CreateLogger<ObservabilityBuilder>();
            }

            if (obsBuilder.Options == null)
                throw new ArgumentNullException("Observability is not configured");

            var options = obsBuilder.Options;
            // Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(obsBuilder.Options));

            if (!options.EnableMetrics && !options.EnableTracing)
                return obsBuilder.Builder;

            if (obsBuilder.Options.ApplicationCard != null && obsBuilder.AppCard == null)
                obsBuilder.AppCard = new ApplicationCard(obsBuilder.Options.ApplicationCard);

            
            if (obsBuilder.AppCard == null)
                throw new ArgumentNullException("Observability Application Card is not configured");

            var configuration = builder.Configuration;
            Uri? otelEndpoint = GetOtelEndpoint(obsBuilder);

            var serviceName = obsBuilder.AppCard.Key ?? "unknown-service";
            var version = obsBuilder.AppCard.Version;

            obsBuilder.Logger.LogInformation($"Observability serviceName: {obsBuilder.AppCard}");
            obsBuilder.Logger.LogInformation($"OTLP EndPoint: {otelEndpoint}");

            var otelBuilder = builder.Services.AddOpenTelemetry();

            otelBuilder
                .ConfigureResource(resource => resource.AddService(
                      serviceName: serviceName,
                      serviceVersion: version,
                      serviceInstanceId: Environment.MachineName)
                  );

            if (options.EnableTracing)
            {
                obsBuilder.Logger.LogDebug("Configuring OpenTelemetry Traces for service {service}", serviceName);
                //TRACES registration
                builder.Services.AddSingleton(new ActivitySource(serviceName, version));
                builder.Services.AddSingleton<ITracesService, TracesService>();
                builder.Services.AddSingleton<ILabelNameBuilder, LabelNameBuilder>();
                builder.Services.AddScoped<IActivityProcess, ActivityProcess>();
                builder.Services.AddScoped<ActivityProcess, ActivityProcess>();

                otelBuilder.ApplyTraces(obsBuilder.Logger, builder, options.Tracing, serviceName, otelEndpoint);
            }

            if (options.EnableMetrics)
            {
                obsBuilder.Logger.LogDebug("Configuring OpenTelemetry Metrics for service {service}", serviceName);
                //Metrics
                builder.Services.AddSingleton(new Meter(serviceName, version));

                var metricsNaming = new List<string>();

                if (!string.IsNullOrWhiteSpace(options.Metrics.Prefix))
                    metricsNaming.Add(options.Metrics.Prefix);
                if (options.Metrics.IncludeServiceName)
                    metricsNaming.Add(serviceName);
                var metricName = string.Join(".", metricsNaming);

                builder.Services.AddSingleton<IMetricNameBuilder>(new MetricNameBuilder(metricName));
                builder.Services.AddSingleton<IMetricsService, MetricsService>();

                otelBuilder.ApplyMetrics(obsBuilder.Logger, builder, options.Metrics, serviceName, otelEndpoint);
            }

            if (options.EnableMetrics)
            {
                builder.Services.AddSingleton<IObservabilityService, ObservabilityService>();
                builder.Services.AddScoped(typeof(IObservabilityService<>), typeof(ObservabilityService<>));
            }

            return builder;

        }
        catch (Exception ex)
        {
            obsBuilder.Logger?.LogError(ex, "Error building Observability for service {service}", obsBuilder.AppCard?.Key ?? "");
            throw;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="otelBuilder"></param>
    /// <param name="log"></param>
    /// <param name="builder"></param>
    /// <param name="optionsMetrics"></param>
    /// <param name="serviceName"></param>
    /// <param name="otelEndpoint"></param>
    /// <returns></returns>
    public static OpenTelemetryBuilder ApplyMetrics(
        this OpenTelemetryBuilder otelBuilder,
        ILogger log,
        IHostApplicationBuilder builder,
        MetricsOptions optionsMetrics,
        string serviceName,
        Uri otelEndpoint)
    {
        var meterName = serviceName; // $"{settings.AppName}-Meter";
        var meters = optionsMetrics.Meters?.ToArray() ?? Array.Empty<string>();

        otelBuilder.WithMetrics(metrics =>
        {
            metrics
              .AddMeter(meterName)
              .ConfigureMeters(meters)
              .AddHttpClientInstrumentation()
              .AddAspNetCoreInstrumentation() //Microsoft.AspNetCore.*
              .AddProcessInstrumentation() //OpenTelemetry.Instrumentation.Process

              .AddPrometheusExporter()
              .SetExemplarFilter(ExemplarFilterType.TraceBased)
              .AddOtlpExporter((options, metricReaderOptions) =>
              {
                  options.Protocol = OtlpExportProtocol.Grpc;
                  options.Endpoint = otelEndpoint;
                  metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = optionsMetrics.ExportIntervalMilliseconds; 
              })
              .AddReader(new PeriodicExportingMetricReader(new OtlpMetricExporter(new OtlpExporterOptions
              {
                  Endpoint = otelEndpoint,
                  Headers = optionsMetrics.Headers, //settings.Headers ??
                  Protocol = OtlpExportProtocol.Grpc
              }))
              {
                  TemporalityPreference = MetricReaderTemporalityPreference.Delta,
              })
              .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));

            if (optionsMetrics.MeterBuilderAction != null)
            {
                try
                {
                    optionsMetrics.MeterBuilderAction.Invoke(metrics);
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error in Observavility.WithTraces event");
                }
            }

            if (optionsMetrics.WithConsoleExporter && builder.Environment.IsDevelopment())
                metrics.AddConsoleExporter();
        });
        return otelBuilder;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="otelBuilder"></param>
    /// <param name="log"></param>
    /// <param name="builder"></param>
    /// <param name="optionsTracing"></param>
    /// <param name="serviceName"></param>
    /// <param name="otelEndpoint"></param>
    /// <returns></returns>
    public static OpenTelemetryBuilder ApplyTraces(
        this OpenTelemetryBuilder otelBuilder,
        ILogger log,
        IHostApplicationBuilder builder,
        TracingOptions optionsTracing,
        string serviceName,
        Uri otelEndpoint)
    {
        var activitySourceName = $"{serviceName}-Activity";

        otelBuilder.WithTracing(traces =>
        {
            if (!string.IsNullOrWhiteSpace(optionsTracing.RedisUrl))
            {
                log.LogDebug("Connecting to Redis for OpenTelemetry Traces in {redisUrl}", optionsTracing.RedisUrl);
                try
                {
                    var connection = ConnectionMultiplexer.Connect(optionsTracing.RedisUrl);
                    builder.Services.AddSingleton<IConnectionMultiplexer>(connection);
                    traces
                    .AddRedisInstrumentation(connection, opt =>
                    {
                        opt.FlushInterval = TimeSpan.FromSeconds(1);
                        opt.SetVerboseDatabaseStatements = true; // opcional
                    });
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error connecting to Redis for OpenTelemetry Traces in {redisUrl}", optionsTracing.RedisUrl);
                }
            }

            traces
                .AddEntityFrameworkCoreInstrumentation()
                .AddSource(activitySourceName)
                .SetSampler(new AlwaysOnSampler())
                .AddAspNetCoreInstrumentation()
                .AddOtlpExporter(o => { o.Protocol = OtlpExportProtocol.Grpc; o.Endpoint = otelEndpoint; });

            if (optionsTracing.TracerBuilderAction != null)
            {
                try
                {
                    optionsTracing.TracerBuilderAction.Invoke(traces);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error in Observavility.WithMetrics event: " + ex.ToString());
                }
            }

            if (optionsTracing.WithConsoleExporter && builder.Environment.IsDevelopment())
            {
                traces.AddConsoleExporter();
            }
        });

        return otelBuilder;
    }

    private static Uri GetOtelEndpoint(ObservabilityBuilder obsBuilder)
    {
        ArgumentException.ThrowIfNullOrEmpty(obsBuilder.OpentelemetryUrl ?? obsBuilder.Options?.OpentelemetryUrl,
            "Observability OpentelemetryUrl is not configured");

        obsBuilder.OpentelemetryUrl = obsBuilder.OpentelemetryUrl ?? obsBuilder?.Options?.OpentelemetryUrl;

        if (obsBuilder?.OpentelemetryUrl == null)
            throw new ArgumentNullException("Observability OpentelemetryUrl is not configured");

        Uri? otelEndpoint;
        if (!Uri.TryCreate(obsBuilder.OpentelemetryUrl, UriKind.Absolute, out otelEndpoint))
            throw new ArgumentNullException("OpentelemetryUrl is not valid");
        return otelEndpoint;
    }

    /// <summary>
    /// configure observability options using a configuration action
    /// </summary>
    /// <param name="obsBuilder">observability builder</param>
    /// <param name="options"><see cref="ObservabilityOptions"/></param>
    /// <returns>The ObservabilityBuilder <see cref="ObservabilityBuilder"/></returns>
    public static ObservabilityBuilder Configure(this ObservabilityBuilder obsBuilder, Func<ObservabilityOptions, ObservabilityOptions> options)
    {
        var settings = ObservabilityOptions.Default();

        obsBuilder.Options = options.Invoke(settings);

        return obsBuilder;
    }

    /// <summary>
    /// Apply metrics configuration action
    /// </summary>
    /// <param name="obsBuilder">observability builder</param>
    /// <param name="action"> action to execute</param>
    /// <returns>The ObservabilityBuilder <see cref="ObservabilityBuilder"/></returns>
    public static ObservabilityBuilder WithMetrics(
        this ObservabilityBuilder obsBuilder, Action<MeterProviderBuilder> action)
    {
        ArgumentNullException.ThrowIfNull(action, nameof(action));
        ArgumentNullException.ThrowIfNull(obsBuilder, nameof(obsBuilder));
        ArgumentNullException.ThrowIfNull(obsBuilder.Options, nameof(obsBuilder.Options));

        obsBuilder.Options.Metrics.MeterBuilderAction = action;

        return obsBuilder;
    }

    /// <summary>
    /// Apply traces configuration action
    /// </summary>
    /// <param name="obsBuilder">observability builder</param>
    /// <param name="action"> action to execute</param>
    /// <returns>The ObservabilityBuilder <see cref="ObservabilityBuilder"/></returns>
    public static ObservabilityBuilder WithTraces(
        this ObservabilityBuilder obsBuilder, Action<TracerProviderBuilder> action)
    {
        ArgumentNullException.ThrowIfNull(action, nameof(action));
        ArgumentNullException.ThrowIfNull(obsBuilder, nameof(obsBuilder));
        ArgumentNullException.ThrowIfNull(obsBuilder.Options, nameof(obsBuilder.Options));
        obsBuilder.Options.Tracing.TracerBuilderAction = action;

        return obsBuilder;
    }
}
