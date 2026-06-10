using System.ComponentModel.DataAnnotations;

namespace WebOpenObserveDemo.Models;

public class VoteDto
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "El identificador de usuario es obligatorio")]
    [Display(Name = "Usuario")]
    public string UserKey { get; set; }

    [Required(ErrorMessage = "Debe seleccionar un producto")]
    [Display(Name = "Producto")]
    public Guid ProductoId { get; set; }

    [Required(ErrorMessage = "La valoración es obligatoria")]
    [Range(1, 5, ErrorMessage = "La valoración debe estar entre 1 y 5")]
    [Display(Name = "Valoración")]
    public int Valor { get; set; }

    [Display(Name = "Comentario")]
    [StringLength(500, ErrorMessage = "El comentario no puede exceder 500 caracteres")]
    public string? Comentario { get; set; }

    [MaxLength(3)]
    [Display(Name = "País")]
    public string? Country { get; set; }

    [Required]
    [Display(Name = "Fecha")]
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public EstadoVoto Estado { get; set; } = EstadoVoto.Pending;
}
public enum EstadoVoto
{
    Pending,
    Approved,
    Rejected
}

public class VoteResultDto
{
    public Guid VoteId { get; set; }
    public string Message { get; set; }
    public double NewAverage { get; set; }
}