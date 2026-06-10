using OpenTelemetry.Trace;

namespace Observability
{
    /// <summary>
    /// Tracing configuration options.
    /// </summary>
    public class TracingOptions
    {
        /// <summary>
        /// Configures whether to use the console exporter for tracing.
        /// </summary>
        public bool WithConsoleExporter { get; set; } = false;

        /// <summary>
        /// Redis connection string for distributed tracing. Empty if not used.
        /// </summary>
        public string? RedisUrl { get; set; } = null;

        internal Action<TracerProviderBuilder>? TracerBuilderAction { get; set; }
    }
}
