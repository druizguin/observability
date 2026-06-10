namespace Rating.BusinessLayer.HostedServices
{
    // [Demo V2] 2.3.1 Ejemplo de contexto

    public class ClientObservabilityContext
    {
        public string Producto { get; set; }
        public string User { get; set; }
        public int Rating { get; set; }
        public string Tenant { get; set; }
        public string Categoria { get; set; }
    }
}