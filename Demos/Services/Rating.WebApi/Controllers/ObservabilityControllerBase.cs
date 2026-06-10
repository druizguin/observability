namespace Rating.BusinessLayer.Controllers;

using Microsoft.AspNetCore.Mvc;
using Unir.Framework.Observability;
using Unir.Framework.Observability.Abstractions;

public class ObservabilityControllerBase : ControllerBase
{
    public IServiceProvider Services { get; private set; }
    internal IObservabilityService _obs { get; private set; }
    public ObservabilityControllerBase(IServiceProvider services)
    {
        Services = services;
        _obs = services.GetRequiredService<IObservabilityService>();
    }
}

public class ObservabilityControllerBase<T> : ControllerBase
{
    public IServiceProvider Services { get; private set; }
    internal IObservabilityService<T> _obs { get; private set; }
    public ObservabilityControllerBase(IServiceProvider services)
    {
        Services = services;
        _obs = services.GetRequiredService<IObservabilityService<T>>();
    }
}
