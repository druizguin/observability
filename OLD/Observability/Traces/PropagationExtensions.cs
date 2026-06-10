using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Observability.Abstractions;

namespace Observability.Abstractions;

/// <summary>
/// The extension methods for trace propagation
/// </summary>
public static class PropagationExtensions
{
    /// <summary>
    /// Correlate trace from incoming RabbitMQ message
    /// </summary>
    /// <param name="builder">The traceBuilder <see cref="TraceBuilder"/></param>
    /// <param name="headers">The headers to transfer to the remote process.</param>
    /// <returns></returns>
    public static TraceBuilder CorrelateFromRabbit(this TraceBuilder builder, IDictionary<string, object?>? headers)
    {
        ArgumentNullException.ThrowIfNull(headers, nameof(headers));

        TextMapPropagator propagator = Propagators.DefaultTextMapPropagator;
        PropagationContext parentContext = propagator.Extract(default, headers, (carrier, key) =>
            {
                if (carrier.TryGetValue(key, out var value))
                {
                    var bytes = value as byte[];
                    if (bytes != null) return new[] { Encoding.UTF8.GetString(bytes) };
                }
                return Enumerable.Empty<string>();
            });

        //Baggage.Current = parentContext.Baggage;
        builder.PropagationContext = parentContext;
        return builder;
    }

    /// <summary>
    /// Correlate trace from incoming HTTP request
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    public static TraceBuilder CorrelateFrom(this TraceBuilder builder, HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));


        if (request.Headers.ContainsKey("x-traceid"))
        {
            if (string.IsNullOrEmpty(request.Headers["x-traceid"])) return builder;
            var headercontent = request.Headers["x-traceid"].ToString();
            var headers = JsonSerializer.Deserialize<Dictionary<string, string?>>(headercontent);

            TextMapPropagator propagator = Propagators.DefaultTextMapPropagator;
            PropagationContext parentContext = propagator.Extract(default, headers, (carrier, key) =>
            {
                if (carrier != null && carrier.TryGetValue(key, out var value))
                {
                    return new[] { value ?? "" };
                }
                return Enumerable.Empty<string>();
            });

            //Baggage.Current = parentContext.Baggage;
            builder.PropagationContext = parentContext;
        }

        return builder;
    }

    /// <summary>
    /// Correlate trace to outgoing HTTP client    
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="client"></param>
    public static void CorrelateTo(this IActivityProcess activity, HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(client, nameof(client));

        var propagator = Propagators.DefaultTextMapPropagator;
        var headers = new Dictionary<string, string?>();

        if (activity.Activity != null)
        {
            propagator.Inject(new
                 PropagationContext(activity.Activity.Context, Baggage.Current),
                 headers,
                 (carrier, key, value) => { carrier[key] = value; });

            client.DefaultRequestHeaders.Add("x-traceid", JsonSerializer.Serialize(headers));
        }
    }

    /// <summary>
    /// Get propagation headers from activity
    /// </summary>
    /// <param name="activity">the selected activity</param>
    /// <returns>A dictionary of String,object with propagation values.</returns>
    public static IDictionary<string, object?> GetPropagationHeaders(this Activity activity)
    {
        ArgumentNullException.ThrowIfNull(activity, nameof(activity));

        var propagator = Propagators.DefaultTextMapPropagator;
        var headers = new Dictionary<string, object?>();

        propagator.Inject(new
             PropagationContext(activity.Context, Baggage.Current),
             headers,
             (carrier, key, value) => { carrier[key] = value; });

        return headers;
    }
}
