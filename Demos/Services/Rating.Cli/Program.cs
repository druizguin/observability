using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using Rating.BusinessLayer.HostedServices;
using Serilog;
using Unir.Framework.Observability;

namespace Rating.Cli;

internal class Program
{
    static async Task Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        var configuration = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json")
               .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
               .AddEnvironmentVariables()
               .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            //.WriteTo.Console()
            //.WriteTo.OpenTelemetry()
            .CreateLogger();

        builder.Logging.ClearProviders();
        builder.Services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(dispose: true);
            loggingBuilder.AddConsole();
        });

        builder
            .CreateObservabilityBuilder()
            .LoadFromConfiguration()
            .UseSerilog()
            .WithTraces(traces =>
            {
                traces.AddHttpClientInstrumentation();
            })
            .BuildObservability();

        builder.Services.AddOptions<ApplicationSettings>()
            .Bind(builder.Configuration.GetSection(ApplicationSettings.SettingsSectionName))
            .ValidateDataAnnotations();

        builder.Services.AddHostedService<RatingGenerator>();

        await builder.Build().RunAsync();
    }
}
