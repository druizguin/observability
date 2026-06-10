using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Observability.Abstractions;


namespace Observability;

/// <summary>
/// Extensiones para la configuración de observabilidad desde Serilog.
/// </summary>
public static class ObservabilityBuilderSerilogExtensions
{
    /// <summary>
    /// Permite leer la sección de configuración de Serilog para extraer la configuración de OpenTelemetry.
    /// </summary>
    /// <param name="obsBuilder"> Builder de configuración de observabilidad <see cref="ObservabilityBuilder"/>.</param>
    /// <param name="sectionName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static ObservabilityBuilder UseSerilog(this ObservabilityBuilder obsBuilder, string sectionName = "Serilog")
    {
        //Cargamos application card desde la config de serilog desde:
        //      Serilog.WriteTo[name:OpenTelemetry].Args["ResourceAttributes"]."service.name"
        var writeToSection = obsBuilder.Builder.Configuration.GetSection($"{sectionName}:WriteTo");

        foreach (var section in writeToSection.GetChildren())
        {
            var name = section.GetValue<string>("Name");

            ArgumentException.ThrowIfNullOrEmpty(name, $"{section.Key} Name is not present");

            if (name.ToLower() == "opentelemetry")
            {
                var serviceName = section.GetSection("Args:ResourceAttributes").GetValue<string>("service.name");
                ArgumentException.ThrowIfNullOrEmpty(serviceName, "Serilog:WriteTo:OpenTelemetry:Args:ResourceAttributes:service.name");

                var endpoint = section.GetSection("Args").GetValue<string>("Endpoint");
                ArgumentException.ThrowIfNullOrEmpty(endpoint, "Serilog:WriteTo:OpenTelemetry:Args:ResourceAttributes:Endpoint");

                obsBuilder.AppCard = ApplicationCardExtensions.BuildAppCard(obsBuilder.Builder, serviceName);
                obsBuilder.OpentelemetryUrl = endpoint;

                return obsBuilder;
            }
        }

        throw new ArgumentException("observability is not configured",
            "Serilog:WriteTo:OpenTelemetry:Args:ResourceAttributes");
    }
}
