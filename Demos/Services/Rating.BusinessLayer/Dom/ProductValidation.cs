using System.ComponentModel.DataAnnotations;

namespace Rating.BusinessLayer.Dom;
public class ProductValidation
{
    [Required]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid ProductoId { get; set; }

    [Required]
    public ProductStatus? ChangetToStatus { get; set; }
    [Required]
    public string User { get; set; }

    public string Comment { get; set; }

    [Required]
    public DateTimeOffset Date { get; set; } = DateTimeOffset.UtcNow;
}