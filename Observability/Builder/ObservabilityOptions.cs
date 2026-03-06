using Observability.Abstractions;

namespace Observability
{
    /// <summary>
    /// Opciones de configuración para la observabilidad.
    /// </summary>
    public class ObservabilityOptions
    {
        /// <summary>
        /// Habilita métricas.
        /// </summary>
        public bool EnableMetrics { get; set; } = true;

        /// <summary>
        /// Habilita trazas.
        /// </summary>  
        public bool EnableTracing { get; set; } = true;

        /// <summary>
        /// Configuración de métricas. Consulte <see cref="MetricsOptions" />.
        /// </summary>  
        public MetricsOptions Metrics { get; set; } = new MetricsOptions();

        /// <summary>
        /// Configuración de trazas. Consulte <see cref="TracingOptions" />.
        /// </summary>  
        public TracingOptions Tracing { get; set; } = new TracingOptions();

        /// <summary>
        /// Configuración de trazas. Consulte <see cref="TracingOptions" />.
        /// </summary>  
        public string? OpentelemetryUrl { get; set; }

        /// <summary>
        /// The application card in AppCard format <see cref="IApplicationCard"/>.
        /// </summary>
        public string? ApplicationCard { get; set; }

        internal static ObservabilityOptions Default()
        {
            return new ObservabilityOptions
            {
                EnableMetrics = true,
                EnableTracing = true
            };
        }
    }

}
