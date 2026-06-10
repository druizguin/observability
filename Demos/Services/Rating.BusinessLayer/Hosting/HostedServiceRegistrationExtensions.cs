using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace Microsoft.AspNetCore.Builder;

public static class HostedServiceRegistrationExtensions
{
    public static WebApplicationBuilder AddBackgroundServices(
    this WebApplicationBuilder builder,
    bool singleton,
    params Type[] services)
    {
        IEnumerable<Type> backgroundServiceTypes = services
        .Where(t =>
        t.IsClass &&
        !t.IsAbstract &&
        typeof(BackgroundService).IsAssignableFrom(t) &&
        t.Name.EndsWith("BackgroundService", StringComparison.OrdinalIgnoreCase));

        if (singleton)
        {
            foreach (var serviceType in backgroundServiceTypes)
            {
                builder.Services.AddSingleton(typeof(IHostedService), serviceType);
            }
        }
        else
        {
            foreach (var serviceType in backgroundServiceTypes)
            {
                builder.Services.AddTransient(typeof(IHostedService), serviceType);
            }
        }

        return builder;
    }

    public static WebApplicationBuilder AddAllBackgroundServices(
        this WebApplicationBuilder builder, 
        Assembly assembly,
        bool singleton = true,
        params Type[] except)
    {
        IEnumerable<Type> backgroundServiceTypes = assembly
        .GetTypes()
        .Where(t =>
        t.IsClass &&
        !t.IsAbstract &&
        typeof(BackgroundService).IsAssignableFrom(t) &&
        t.Name.EndsWith("BackgroundService", StringComparison.OrdinalIgnoreCase))
        .Except(except);

        builder.AddBackgroundServices(singleton, backgroundServiceTypes.ToArray());

        return builder;
    }
}

