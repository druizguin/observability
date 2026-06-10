namespace Observability.Demo.Controllers
{
    public class MetricsContext 
    {
        public MetricsContext(string proceso, string version)
        {
            Proceso= proceso;
            Version = version;
        }
        public string Proceso { get;  set; }

        public string Version { get; set; }
    }
}
