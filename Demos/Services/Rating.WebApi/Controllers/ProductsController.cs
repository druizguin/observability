namespace Rating.BusinessLayer.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rating.BusinessLayer.Data;
using Rating.BusinessLayer.Dom;
using Rating.BusinessLayer.Models;
using System.Diagnostics;
using Unir.Framework.Observability;
using Unir.Framework.Observability.Abstractions;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ObservabilityControllerBase
{
    private readonly VotingDbContext _context;
    private readonly ILogger _logger;

    public ProductsController(VotingDbContext context, IServiceProvider services) : base(services)
    {
        _context = context;
        _logger = base._obs.Logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create(Product product)
    {
        _logger.LogInformation("Product.Create Creating product {ProductName}", product.Name);

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> Get(Guid id)
    {
        _logger.LogInformation($"Get({id}) Obtener producto");
        var product = await _context.Products.Include(p => p.Votes).FirstOrDefaultAsync(p => p.Id == id);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpGet()]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll()
    {
        _logger.LogInformation("Headers recibidos: {Headers}", string.Join(", ", Request.Headers.Select(h => $"{h.Key}={h.Value}")));

        var result =
            _obs.Traces
            .Configure($"2.0 Obtener productos", ActivityKind.Server)
            .CorrelateFrom(this.Request) // [Demo V2] 2.3.3.2 Code.Traces: Correlación de actividades (HttpClient) CorrelateFrom
            .Build()
            .Execute(activity =>
            {
                _logger.LogInformation("GetAll: Obtener el listado de productos");
                var products = _context.Products.ToArrayAsync().GetAwaiter().GetResult();
                return Ok(products);
            });

        return await Task.FromResult(result);
    }

    [HttpGet("bff")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAllBff()
    {
        var result =
            _obs.Traces
            .Configure($"2.0 Obtener productos", ActivityKind.Server)
            .CorrelateFrom(this.Request) // [Demo V2] 2.3.3.2 Code.Traces: Correlación de actividades (HttpClient) CorrelateFrom
            .Build()
            .Execute(activity =>
            {
                _logger.LogInformation("GetAll: Obtener el listado de productos");
                var products = _context.Products.Include(p => p.Votes).ToArrayAsync().GetAwaiter().GetResult();

                var dtos = products.Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Category = p.Category,
                    Status = p.Status,
                    TotalVotes = p.Votes.Count,
                    AverageRating = p.Votes.Count > 0 ? p.Votes.Average(v => v.Valor) : 0
                }).ToArray();

                return Ok(dtos);
            });




        return await Task.FromResult(result);
    }
}
