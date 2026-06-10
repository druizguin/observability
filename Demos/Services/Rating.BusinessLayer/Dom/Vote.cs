using Rating.BusinessLayer.HostedServices;
using System.ComponentModel.DataAnnotations;

namespace Rating.BusinessLayer.Dom;

public class VoteDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserKey { get; set; }
    public Guid ProductoId { get; set; }
    
    [Required]
    [Range(1, 5)]
    public int Valor { get; set; } // 1-5
    public string? Comentario { get; set; }

    [MaxLength(3)]
    public string? Country { get; set; }

    [Required]
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public EstadoVoto Estado { get; set; } = EstadoVoto.Pending;
}

public class Vote : VoteDto
{
   
    public Product? Producto { get; set; }
}
