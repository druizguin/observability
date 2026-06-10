namespace Observability.Abstractions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

/// <summary>
/// Exte
/// </summary>
public static class ApplicationCardExtensions
{
    internal static IApplicationCard BuildAppCard(IHostApplicationBuilder builder, string serviceName)
    {
        var appCard = new ApplicationCard(serviceName);

        var settings = new Dictionary<string, string?>
            {
                { "ApplicationCard:Key", appCard.Key },
                { "ApplicationCard:Version", appCard.Version },
                { "ApplicationCard:Entorno", appCard.Entorno }
            };

        builder.Configuration.AddInMemoryCollection(settings);
        builder.Services.AddSingleton<IOptions<IApplicationCard>>(Options.Create(appCard));

        return appCard;
    }
}