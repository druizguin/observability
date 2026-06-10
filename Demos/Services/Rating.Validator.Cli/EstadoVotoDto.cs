using System.ComponentModel.DataAnnotations;

namespace Rating.BusinessLayer.HostedServices;

public enum EstadoVotoDto
{
    Pending,
    Approved,
    Rejected
}

public class RatingDto
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ProductoId { get; set; }

    [Required]
    public string UserKey { get; set; }

    [Range(1, 5)]
    public int Valor { get; set; }

    public string? Comentario { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}