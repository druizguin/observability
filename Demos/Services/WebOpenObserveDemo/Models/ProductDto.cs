using System.ComponentModel.DataAnnotations;

namespace WebOpenObserveDemo.Models;

public class ProductDto
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "El nombre del producto es obligatorio")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres")]
    [Display(Name = "Nombre del Producto")]
    public string Name { get; set; }

    [Required(ErrorMessage = "La categoría es obligatoria")]
    [Display(Name = "Categoría")]
    public string Category { get; set; }

    [Display(Name = "Estado")]
    public ProductStatus Status { get; set; } = ProductStatus.Draft;

    public int TotalVotes { get; set; }
    public double AverageRating { get; set; }
}

public class ProductCreateDto
{
    [Required(ErrorMessage = "El nombre del producto es obligatorio")]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; }

    [Required(ErrorMessage = "La categoría es obligatoria")]
    public string Category { get; set; }
}

public enum ProductStatus
{
    Draft,
    Validated,
    Approved,
    Active,
    Inactive,
    Discontinued
}