using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using Serilog;
using Unir.Framework.Observability;

namespace Rating.Validator.Cli;

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
        });
        
        builder
            .CreateObservabilityBuilder()
                .LoadFromConfiguration()
                .UseSerilog()
                .WithMetrics(metrics => {
                    //Añadir métricas específicas del proyecto
                    metrics.AddHttpClientInstrumentation();
                })
                .BuildObservability();

        builder.Services.AddHttpClient();

        builder.Services.AddOptions<ApplicationSettings>()
           .Bind(builder.Configuration.GetSection(ApplicationSettings.SettingsSectionName))
           .ValidateDataAnnotations();
        builder.Services.AddHostedService<VoteApprovalService>();

        Console.WriteLine("Settings:ServiceUrl:" + builder.Configuration.GetSection(ApplicationSettings.SettingsSectionName)["ServiceUrl"]);

        Console.WriteLine("Service name: " + builder.Configuration.GetSection("Serilog:WriteTo[1]:Args:ResourceAttributes")["service.name"]);
        await builder.Build().RunAsync();
    }
}
