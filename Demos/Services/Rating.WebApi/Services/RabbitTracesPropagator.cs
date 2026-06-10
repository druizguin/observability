namespace Rating.BusinessLayer.Services;

using RabbitMQ.Client;
using System.Diagnostics;
using Unir.Framework.Observability.Abstractions;

public class RabbitTracesPropagator 
{
    public static BasicProperties CorrelateTo(Activity? activity, BasicProperties? properties = null)
    {
        properties = properties ?? new BasicProperties();
        properties.Headers = activity?.GetPropagationHeaders();

        return properties;
    }
}
