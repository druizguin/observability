using System.ComponentModel.DataAnnotations;

namespace Rating.BusinessLayer.HostedServices;

public class RabbitRatingDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Producto { get; set; }
    public string Categoria { get; set; }

    [Required]
    public string UserKey { get; set; }

    [Required]
    [Range(1, 5)]
    public int Valor { get; set; } // 1-5
    public string? Comentario { get; set; }
    [Required]
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
