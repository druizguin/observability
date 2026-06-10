using System.ComponentModel.DataAnnotations;

namespace Rating.Cli.Dtos;


public class ProductDTO
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    [MinLength(3)]
    public string Name { get; set; }

    [Required]
    public string Category { get; set; }

    [Required]
    public ProductStatus Status { get; set; } = ProductStatus.Draft;
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