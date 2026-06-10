namespace Observability;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Observability.Abstractions;

/// <summary>
/// Creates and configures observability services.
/// </summary>
public class ObservabilityBuilder
{
    /// <summary>
    /// The host application builder used to configure services.
    /// </summary>
    internal IHostApplicationBuilder Builder { get; private set; }

    /// <summary>
    /// The observability options used to configure the observability services.
    /// </summary>
    public ObservabilityOptions? Options { get; internal set; }

    /// <summary>
    /// the application card that describes the running application (key, version, environment).
    /// </summary>
    public IApplicationCard? AppCard { get; internal set; }

    /// <summary>
    /// The OpenTelemetry collector URL or target TOTP application used to export traces, metrics and logs.
    /// </summary>
    public string? OpentelemetryUrl { get; internal set; } //From Serilog

    internal ILogger<ObservabilityBuilder>? Logger { get; set; }

    internal ObservabilityBuilder(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        Builder = builder;
    }
}
