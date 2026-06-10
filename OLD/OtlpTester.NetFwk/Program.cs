
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.Metrics;

namespace OtlpTester.NetFwk
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("OtlpTester starts");

            var builder = Host.CreateApplicationBuilder(args);

            // Lee configuración de servicio y OTLP
            var serviceName = builder.Configuration.GetValue<string>("Service:Name") ?? "OtelConsoleDemo";
            var serviceVersion = builder.Configuration.GetValue<string>("Service:Version") ?? "1.0.0";
            var otlpSection = builder.Configuration.GetSection("Otlp");
            var baseUrl = otlpSection.GetValue<string>("BaseUrl") ?? "http://localhost";
            var otlpHeaders = otlpSection.GetValue<string>("Headers");

            // Lee los endpoints configurados
            var endpoints = otlpSection.GetSection("Endpoints").Get<List<OtlpEndpointConfig>>() ?? new List<OtlpEndpointConfig>
            {
                new OtlpEndpointConfig { Port = 4318, Protocol = "http", Name = "Default Http" }
            };

            Console.WriteLine($"Configured {endpoints.Count} OTLP endpoint(s):");
            foreach (var endpoint in endpoints)
            {
                Console.WriteLine($"  - {endpoint.Name}: {baseUrl}:{endpoint.Port} ({endpoint.Protocol})");
            }

            // ---------- OpenTelemetry: Resource ----------
            var resourceBuilder = ResourceBuilder.CreateDefault()
                .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
                .AddAttributes(new[]
                {
                    new KeyValuePair<string, object>("deployment.environment", "dev"),
                    new KeyValuePair<string, object>("host.name", Environment.MachineName),
                });

            // ---------- OpenTelemetry: Traces & Metrics ----------
            builder.Services.AddOpenTelemetry()
                 .ConfigureResource(resource => resource.AddService(
                          serviceName: serviceName,
                          serviceVersion: serviceVersion,
                          serviceInstanceId: Environment.MachineName)
                      )
                .WithTracing(tracerProviderBuilder =>
                {
                    tracerProviderBuilder
                        .AddSource(serviceName) // Nuestro ActivitySource
                        .SetSampler(new AlwaysOnSampler());

                    // Añadir un exporter por cada endpoint configurado
                    foreach (var endpoint in endpoints)
                    {
                        tracerProviderBuilder.AddOtlpExporter($"otlp-traces-{endpoint.Port}", options =>
                        {
                            options.Protocol = endpoint.Protocol?.ToLowerInvariant() == "http"
                                ? OtlpExportProtocol.HttpProtobuf
                                : OtlpExportProtocol.Grpc;
                            options.Endpoint = SetEndpointByProtocol(baseUrl, endpoint.Port, "traces", options.Protocol);

                            if (!string.IsNullOrWhiteSpace(otlpHeaders))
                            {
                                options.Headers = otlpHeaders;
                            }
                        });
                    }
                })
                .WithMetrics(meterProviderBuilder =>
                {
                    meterProviderBuilder
                        .AddMeter(serviceName) // Nuestro Meter
                        .AddRuntimeInstrumentation()
                        .AddProcessInstrumentation();

                    // Añadir un exporter por cada endpoint configurado
                    foreach (var endpoint in endpoints)
                    {
                        meterProviderBuilder.AddOtlpExporter($"otlp-metrics-{endpoint.Port}", options =>
                        {
                            options.Protocol = endpoint.Protocol?.ToLowerInvariant() == "http"
                                    ? OtlpExportProtocol.HttpProtobuf
                                    : OtlpExportProtocol.Grpc;
                            options.Endpoint = SetEndpointByProtocol(baseUrl, endpoint.Port, "metrics", options.Protocol);

                            if (!string.IsNullOrWhiteSpace(otlpHeaders))
                            {
                                options.Headers = otlpHeaders;
                            }
                        });
                    }
                });

            // ---------- OpenTelemetry: Logs ----------
            builder.Logging.ClearProviders();
            builder.Logging.AddSimpleConsole(opt =>
            {
                opt.TimestampFormat = "HH:mm:ss ";
                opt.SingleLine = true;
            });
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeScopes = true;
                logging.ParseStateValues = true;
                logging.IncludeFormattedMessage = true;

                // Añadir un exporter por cada endpoint configurado
                foreach (var endpoint in endpoints)
                {
                    logging.AddOtlpExporter($"otlp-logs-{endpoint.Port}", options =>
                    {
                        options.Protocol = endpoint.Protocol?.ToLowerInvariant() == "http"
                            ? OtlpExportProtocol.HttpProtobuf
                            : OtlpExportProtocol.Grpc;
                        options.Endpoint = SetEndpointByProtocol(baseUrl, endpoint.Port, "logs", options.Protocol);

                        if (!string.IsNullOrWhiteSpace(otlpHeaders)) options.Headers = otlpHeaders;
                    });
                }
            });

            using (var host = builder.Build())
            {

                // Obtenemos dependencias
                var logger = host.Services.GetRequiredService<ILogger<Program>>();
                var tracerProvider = host.Services.GetRequiredService<TracerProvider>();
                var meterProvider = host.Services.GetRequiredService<MeterProvider>();

                // Fuentes de telemetría (nombre debe coincidir con AddSource/AddMeter)
                using (var activitySource = new ActivitySource(serviceName))
                {
                    {
                        var meter = new Meter(serviceName);

                        // ====== MÉTRICAS (dos instrumentos): Counter y Histogram ======
                        var pedidosCounter = meter.CreateCounter<long>(
                            name: "app.pedidos_total",
                            unit: "1",
                            description: "Número total de pedidos procesados (simulado)");

                        var duracionHist = meter.CreateHistogram<double>(
                            name: "app.pedido_duracion_ms",
                            unit: "ms",
                            description: "Duración del procesamiento de pedido (simulado)");

                        // ====== LOGS (dos logs) ======
                        logger.LogInformation("Arrancando demo OpenTelemetry para {ServiceName} v{Version}", serviceName, serviceVersion);
                        logger.LogInformation("Enviando telemetría a {EndpointCount} endpoint(s)", endpoints.Count);

                        // ====== TRAZAS (dos pares padre-hija) ======

                        void SimularProcesoPedido(int pedidoId)
                        {
                            using (var parent = activitySource.StartActivity("procesar-pedido", ActivityKind.Server))
                            {
                                parent?.SetTag("pedido.id", pedidoId);
                                parent?.SetTag("origen", "demo");


                                // métrica 1: Counter (sumamos 1 por pedido)
                                pedidosCounter.Add(1, new KeyValuePair<string, object>("estado", "iniciado"));

                                // Hija: validar-pedido
                                using (var child = activitySource.StartActivity("validar-pedido", ActivityKind.Internal))
                                {
                                    child?.SetTag("validacion.tipo", "reglas-negocio");
                                    // simulamos trabajo
                                    Task.Delay(80).Wait();
                                }

                                // simulamos procesamiento adicional
                                var sw = Stopwatch.StartNew();
                                Task.Delay(60).Wait();
                                sw.Stop();

                                // métrica 2: Histogram (registramos duración en ms)
                                duracionHist.Record(sw.Elapsed.TotalMilliseconds,
                                    new KeyValuePair<string, object>("resultado", "ok"));

                                parent?.SetStatus(ActivityStatusCode.Ok);
                                pedidosCounter.Add(1, new KeyValuePair<string, object>("estado", "finalizado"));

                            }
                        }

                        // Primer par (padre/hija)
                        SimularProcesoPedido(1001);

                        // Segundo par (padre/hija) — para dejar claro que hay plural
                        SimularProcesoPedido(1002);
                    }


                    // Forzamos flush para que la demo exporte antes de salir
                    tracerProvider.ForceFlush();
                    meterProvider.ForceFlush();

                    // Pequeña pausa para permitir que el export batch procese (sobre todo logs/metrics)
                    Task.Delay(2000);

                    Console.WriteLine("OtlpTester completed - telemetry sent to all endpoints");
                }
            }
        }

        private static Uri SetEndpointByProtocol(string baseUrl, int port, string path, OtlpExportProtocol protocol, string version = "v1")
        {
            var url = $"{baseUrl.TrimEnd('/')}:{port}";
            if (protocol == OtlpExportProtocol.HttpProtobuf)
                return new Uri($"{url}/{version}/{path}");
            else
                return new Uri(url);
        }
    }

    internal class OtlpEndpointConfig
    {
        public int Port { get; set; }
        public string Protocol { get; set; } = "grpc";
        public string Name { get; set; } = "OTLP Endpoint";
    }

}
