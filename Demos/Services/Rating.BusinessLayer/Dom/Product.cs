using System.ComponentModel.DataAnnotations;

namespace Rating.BusinessLayer.Dom;

public class Product
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

    public List<Vote> Votes { get; set; } = new List<Vote>();
    public List<ProductValidation> Validations { get; set; } = new List<ProductValidation>();
}
