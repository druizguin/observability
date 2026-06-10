namespace Rating.BusinessLayer.Controllers;

public class RatingObservabilityContext
{
    public string Producto { get; set; }
    public string Categoria { get; set; }

    public int Valor { get; set; }
    public string Usuario { get; set; }
    public DateTime Fecha { get; internal set; }
    public string? Comentario { get; internal set; }
    public string Tenant { get; internal set; }
    public string RabbitQueue { get; set; }
    public string RabbitRol { get; set; }
    public string? Pais { get; internal set; }
}