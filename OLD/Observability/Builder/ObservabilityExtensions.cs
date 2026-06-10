namespace Observability;

using OpenTelemetry.Metrics;

/// <summary>
/// Helper extension methods used internally by the observability builder to configure meters.
/// </summary>
internal static class ObservabilityExtensions
{
    /// <summary>
    /// Configure meters helper delegating to the overload with .NET major version.
    /// </summary>
    internal static MeterProviderBuilder ConfigureMeters(this MeterProviderBuilder builder, params string[] openTelemetrySettingsMetrics)
    {
        return builder.ConfigureMeters(Environment.Version.Major, openTelemetrySettingsMetrics);
    }

    /// <summary>
    /// Configures a list of meters on the provided <see cref="MeterProviderBuilder"/>, including special handling for System.Runtime across .NET versions.
    /// </summary>
    /// <param name="builder">the MeterProviderBuilder</param>
    /// <param name="NetVersion">Major .NET version used to determine runtime instrumentation naming.</param>
    /// <param name="openTelemetrySettingsMetrics">the set of convention metrics.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    internal static MeterProviderBuilder ConfigureMeters(this MeterProviderBuilder builder, 
        int NetVersion, 
        params string[] openTelemetrySettingsMetrics)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        ArgumentNullException.ThrowIfNull(openTelemetrySettingsMetrics, nameof(openTelemetrySettingsMetrics));

        if (openTelemetrySettingsMetrics.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Meters cannot be null or empty.", nameof(openTelemetrySettingsMetrics));
        }

        if (openTelemetrySettingsMetrics.Contains("System.Runtime"))
        {
            openTelemetrySettingsMetrics = openTelemetrySettingsMetrics.Except(openTelemetrySettingsMetrics.Where(m => m == "System.Runtime")).ToArray();

            if (NetVersion >= 9)
            {
                builder.AddMeter("System.Runtime");
            }
            else
            {
                builder.AddMeter("System.Runtime", "OpenTelemetry.Instrumentation.Runtime");
            }
        }

        foreach (var meter in openTelemetrySettingsMetrics)
        {
            builder.AddMeter(meter);
        }

        return builder;
    }
}
