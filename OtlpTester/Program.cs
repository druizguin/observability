namespace OtlpTester
{
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
    using System.Diagnostics.Metrics;

    internal class Program
    {
        //https://github.com/open-telemetry/opentelemetry-proto/tree/main/examples
        static async Task Main(string[] args)
        {
            Console.WriteLine("OtlpTester starts");

            var builder = Host.CreateApplicationBuilder(args);

            // Lee configuración de servicio y OTLP
            var serviceName = builder.Configuration.GetValue<string>("Service:Name") ?? "OtelConsoleDemo";
            var serviceVersion = builder.Configuration.GetValue<string>("Service:Version") ?? "1.0.0";
            var otlpSection = builder.Configuration.GetSection("Otlp");
            var otlpEndpoint = otlpSection.GetValue<string>("Endpoint") ?? "http://localhost:4317";
            var otlpProtocol = (otlpSection.GetValue<string>("Protocol") ?? "grpc")?.Trim().ToLowerInvariant();
            var otlpHeaders = otlpSection.GetValue<string>("Headers");


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
                        .SetSampler(new AlwaysOnSampler())

                        // (Opcional) Añadir instrumentaciones adicionales (HttpClient, ASP.NET Core, etc.)
                        .AddOtlpExporter(options =>
                        {
                            options.Protocol = otlpProtocol == "http"
                                ? OtlpExportProtocol.HttpProtobuf
                                : OtlpExportProtocol.Grpc;
                            options.Endpoint = SetEnpointByProtocol(otlpEndpoint, "traces", options.Protocol);

                            if (!string.IsNullOrWhiteSpace(otlpHeaders))
                            {
                                options.Headers = otlpHeaders;
                            }
                        });
                })
                .WithMetrics(meterProviderBuilder =>
                {
                    meterProviderBuilder
                        .AddMeter(serviceName) // Nuestro Meter
                        .AddRuntimeInstrumentation()
                        .AddProcessInstrumentation()
                        .AddOtlpExporter(options =>
                        {
                            options.Protocol = otlpProtocol == "http"
                                    ? OtlpExportProtocol.HttpProtobuf
                                    : OtlpExportProtocol.Grpc;
                            options.Endpoint = SetEnpointByProtocol(otlpEndpoint, "metrics", options.Protocol);
                       
                            if (!string.IsNullOrWhiteSpace(otlpHeaders))
                            {
                                options.Headers = otlpHeaders;
                            }
                        });
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
                logging.AddOtlpExporter(options =>
                {
                    options.Protocol = otlpProtocol == "http"
                        ? OtlpExportProtocol.HttpProtobuf
                        : OtlpExportProtocol.Grpc;
                    options.Endpoint = SetEnpointByProtocol(otlpEndpoint, "logs", options.Protocol);
                   
                    if (!string.IsNullOrWhiteSpace(otlpHeaders)) options.Headers = otlpHeaders;
                });
            });

            using var host = builder.Build();

            // Obtenemos dependencias
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            var tracerProvider = host.Services.GetRequiredService<TracerProvider>();
            var meterProvider = host.Services.GetRequiredService<MeterProvider>();

            // Fuentes de telemetría (nombre debe coincidir con AddSource/AddMeter)
            using var activitySource = new ActivitySource(serviceName);
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
            logger.LogInformation("Enviando dos trazas (padre/hija) y dos métricas (Counter/Histogram) hacia el Collector");

            // ====== TRAZAS (dos pares padre-hija) ======

            void SimularProcesoPedido(int pedidoId)
            {
                using var parent = activitySource.StartActivity("procesar-pedido", ActivityKind.Server);
                parent?.SetTag("pedido.id", pedidoId);
                parent?.SetTag("origen", "demo");

                // métrica 1: Counter (sumamos 1 por pedido)
                pedidosCounter.Add(1, new KeyValuePair<string, object?>("estado", "iniciado"));

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
                    new KeyValuePair<string, object?>("resultado", "ok"));

                parent?.SetStatus(ActivityStatusCode.Ok);
                pedidosCounter.Add(1, new KeyValuePair<string, object?>("estado", "finalizado"));
            }

            // Primer par (padre/hija)
            SimularProcesoPedido(1001);

            // Segundo par (padre/hija) — para dejar claro que hay plural
            SimularProcesoPedido(1002);

            // Forzamos flush para que la demo exporte antes de salir
            tracerProvider.ForceFlush();
            meterProvider.ForceFlush();

            // Pequeña pausa para permitir que el export batch procese (sobre todo logs/metrics)
            await Task.Delay(2000);

        }

        private static Uri SetEnpointByProtocol(string url, string path, OtlpExportProtocol protocol)
        {
            if (protocol == OtlpExportProtocol.HttpProtobuf)
                return new Uri($"{url.TrimEnd('/')}/v1/{path}");
            else
                return new Uri($"{url.TrimEnd('/')}");
        }
    }
}
