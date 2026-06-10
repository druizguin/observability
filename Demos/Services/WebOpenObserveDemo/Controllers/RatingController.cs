using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebOpenObserveDemo.Models;
using WebOpenObserveDemo.Services;

namespace WebOpenObserveDemo.Controllers;

public class RatingController : Controller
{
    private readonly RatingApiService _ratingService;
    private readonly ILogger<RatingController> _logger;
    private static readonly ActivitySource ActivitySource = new("WebOpenObserveDemo.Rating");

    public RatingController(
        RatingApiService ratingService,
        ILogger<RatingController> logger)
    {
        _ratingService = ratingService;
        _logger = logger;
    }

    // ========================================================================
    // PRODUCTOS - Vistas
    // ========================================================================

    [HttpGet]
    public async Task<IActionResult> Products()
    {
        using var activity = ActivitySource.StartActivity("Rating.Products.Index");
        activity?.SetTag("page", "products.list");

        try
        {
            _logger.LogInformation("Accediendo a la página de productos");
            var products = await _ratingService.GetAllProductsAsync();

            activity?.SetTag("products.count", products.Count);
            return View(products);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error al cargar la página de productos");
            TempData["Error"] = "Error al cargar los productos. Por favor, intente de nuevo.";
            return View(new List<ProductDto>());
        }
    }

    [HttpGet]
    public IActionResult CreateProduct()
    {
        using var activity = ActivitySource.StartActivity("Rating.Products.CreateView");
        activity?.SetTag("page", "products.create");

        _logger.LogInformation("Accediendo al formulario de creación de producto");
        return View(new ProductCreateDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(ProductCreateDto product)
    {
        using var activity = ActivitySource.StartActivity("Rating.Products.Create");
        activity?.SetTag("product.name", product.Name);
        activity?.SetTag("product.category", product.Category);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Modelo inválido al crear producto");
            return View(product);
        }

        try
        {
            _logger.LogInformation("Creando producto {ProductName}", product.Name);
            var created = await _ratingService.CreateProductAsync(product);

            activity?.SetTag("product.id", created.Id.ToString());
            TempData["Success"] = $"Producto '{created.Name}' creado con éxito";

            return RedirectToAction(nameof(Products));
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error al crear producto {ProductName}", product.Name);
            ModelState.AddModelError("", "Error al crear el producto. Por favor, intente de nuevo.");
            return View(product);
        }
    }

    [HttpGet]
    public async Task<IActionResult> ProductDetails(Guid id)
    {
        using var activity = ActivitySource.StartActivity("Rating.Products.Details");
        activity?.SetTag("product.id", id.ToString());

        try
        {
            _logger.LogInformation("Accediendo a detalles del producto {ProductId}", id);
            var product = await _ratingService.GetProductAsync(id);

            if (product == null)
            {
                _logger.LogWarning("Producto {ProductId} no encontrado", id);
                TempData["Error"] = "Producto no encontrado";
                return RedirectToAction(nameof(Products));
            }

            activity?.SetTag("product.name", product.Name);
            return View(product);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error al cargar detalles del producto {ProductId}", id);
            TempData["Error"] = "Error al cargar los detalles del producto";
            return RedirectToAction(nameof(Products));
        }
    }

    // ========================================================================
    // VOTACIONES - Vistas
    // ========================================================================

    [HttpGet]
    public async Task<IActionResult> Vote()
    {
        using var activity = ActivitySource.StartActivity("Rating.Vote.Index");
        activity?.SetTag("page", "vote.form");

        try
        {
            _logger.LogInformation("Accediendo al formulario de votación");
            var products = await _ratingService.GetAllProductsAsync();

            ViewBag.Products = products;
            return View(new VoteDto());
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error al cargar el formulario de votación");
            TempData["Error"] = "Error al cargar el formulario";
            return View(new VoteDto());
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Vote(VoteDto vote)
    {
        using var activity = ActivitySource.StartActivity("Rating.Vote.Submit");
        activity?.SetTag("vote.productId", vote.ProductoId.ToString());
        activity?.SetTag("vote.userKey", vote.UserKey);
        activity?.SetTag("vote.value", vote.Valor);

        if (string.IsNullOrEmpty(vote.Country)) vote.Country = "ESP";

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Modelo inválido al enviar voto");
            var products = await _ratingService.GetAllProductsAsync();
            ViewBag.Products = products;
            return View(vote);
        }

        try
        {
            _logger.LogInformation("Enviando voto para producto {ProductId}", vote.ProductoId);
            var result = await _ratingService.SubmitVoteAsync(vote);

            activity?.SetTag("vote.id", result.VoteId.ToString());
            TempData["Success"] = "¡Voto registrado con éxito!";

            return RedirectToAction(nameof(VoteSuccess), new { voteId = result.VoteId });
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error al enviar voto para producto {ProductId}", vote.ProductoId);
            ModelState.AddModelError("", "Error al registrar el voto. Por favor, intente de nuevo.");

            var products = await _ratingService.GetAllProductsAsync();
            ViewBag.Products = products;
            return View(vote);
        }
    }

    [HttpGet]
    public IActionResult VoteSuccess(Guid voteId)
    {
        using var activity = ActivitySource.StartActivity("Rating.Vote.Success");
        activity?.SetTag("vote.id", voteId.ToString());

        _logger.LogInformation("Mostrando página de éxito para voto {VoteId}", voteId);
        ViewBag.VoteId = voteId;
        return View();
    }

    // ========================================================================
    // API Endpoints para JavaScript
    // ========================================================================

    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        try
        {
            var products = await _ratingService.GetAllProductsAsync();
            return Json(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en API GetProducts");
            return StatusCode(500, new { error = "Error al obtener productos" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> SubmitVoteApi([FromBody] VoteDto vote)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _ratingService.SubmitVoteAsync(vote);
            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en API SubmitVote");
            return StatusCode(500, new { error = "Error al registrar el voto" });
        }
    }
}