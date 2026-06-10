using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using WebOpenObserveDemo.Models;

namespace WebOpenObserveDemo.Services;

public class RatingApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RatingApiService> _logger;
    private readonly IConfiguration _configuration;
    private static readonly ActivitySource ActivitySource = new("WebOpenObserveDemo.RatingApi");

    public RatingApiService(
        HttpClient httpClient,
        ILogger<RatingApiService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    // ========================================================================
    // PRODUCTOS
    // ========================================================================

    public async Task<List<ProductDto>> GetAllProductsAsync()
    {
        // La propagación se hace automáticamente por HttpClientInstrumentation
        // pero podemos añadir contexto adicional
        using var activity = ActivitySource.StartActivity("RatingApi.GetAllProducts", ActivityKind.Client);
        activity?.SetTag("api.operation", "GetProducts");
        activity?.SetTag("api.endpoint", "/api/products/bff");
        activity?.SetTag("api.service", "rating.webapi");

        try
        {
            _logger.LogInformation("Obteniendo lista de productos desde Rating API");

            var response = await _httpClient.GetAsync("/api/products/bff");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var products = JsonSerializer.Deserialize<List<ProductDto>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<ProductDto>();

            activity?.SetTag("products.count", products.Count);
            activity?.SetStatus(ActivityStatusCode.Ok);
            _logger.LogInformation("Obtenidos {Count} productos", products.Count);

            return products;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            _logger.LogError(ex, "Error al obtener productos desde Rating API");
            throw;
        }
    }

    public async Task<ProductDto?> GetProductAsync(Guid id)
    {
        using var activity = ActivitySource.StartActivity("RatingApi.GetProduct", ActivityKind.Client);
        activity?.SetTag("api.operation", "GetProduct");
        activity?.SetTag("api.endpoint", $"/api/products/{id}");
        activity?.SetTag("api.service", "rating.webapi");
        activity?.SetTag("product.id", id.ToString());

        try
        {
            _logger.LogInformation("Obteniendo producto {ProductId} desde Rating API", id);

            var response = await _httpClient.GetAsync($"/api/products/{id}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                activity?.SetStatus(ActivityStatusCode.Ok);
                _logger.LogWarning("Producto {ProductId} no encontrado", id);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var product = JsonSerializer.Deserialize<ProductDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            activity?.SetTag("product.name", product?.Name);
            activity?.SetStatus(ActivityStatusCode.Ok);
            _logger.LogInformation("Producto {ProductId} obtenido: {ProductName}", id, product?.Name);

            return product;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            _logger.LogError(ex, "Error al obtener producto {ProductId}", id);
            throw;
        }
    }

    public async Task<ProductDto> CreateProductAsync(ProductCreateDto product)
    {
        using var activity = ActivitySource.StartActivity("RatingApi.CreateProduct", ActivityKind.Client);
        activity?.SetTag("api.operation", "CreateProduct");
        activity?.SetTag("api.endpoint", "/api/products");
        activity?.SetTag("api.service", "rating.webapi");
        activity?.SetTag("product.name", product.Name);
        activity?.SetTag("product.category", product.Category);

        try
        {
            _logger.LogInformation("Creando producto {ProductName} en categoría {Category}", 
                product.Name, product.Category);

            var json = JsonSerializer.Serialize(product);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/products", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var createdProduct = JsonSerializer.Deserialize<ProductDto>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            activity?.SetTag("product.id", createdProduct?.Id.ToString());
            activity?.SetStatus(ActivityStatusCode.Ok);
            _logger.LogInformation("Producto creado exitosamente: {ProductId}", createdProduct?.Id);

            return createdProduct!;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            _logger.LogError(ex, "Error al crear producto {ProductName}", product.Name);
            throw;
        }
    }

    // ========================================================================
    // VOTACIONES
    // ========================================================================

    public async Task<VoteResultDto> SubmitVoteAsync(VoteDto vote)
    {
        using var activity = ActivitySource.StartActivity("RatingApi.SubmitVote", ActivityKind.Client);
        activity?.SetTag("api.operation", "SubmitVote");
        activity?.SetTag("api.endpoint", "/api/voting");
        activity?.SetTag("api.service", "rating.webapi");
        activity?.SetTag("vote.productId", vote.ProductoId.ToString());
        activity?.SetTag("vote.userKey", vote.UserKey);
        activity?.SetTag("vote.value", vote.Valor);
        activity?.SetTag("vote.country", vote.Country);

        try
        {
            _logger.LogInformation("Enviando voto para producto {ProductId} por usuario {UserKey} con valor {Value}",
                vote.ProductoId, vote.UserKey, vote.Valor);

            var json = JsonSerializer.Serialize(vote);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Add("X-tenant", "Web");

            var response = await _httpClient.PutAsync("/api/voting", content);
            
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                activity?.SetStatus(ActivityStatusCode.Error, $"HTTP {response.StatusCode}");
                _logger.LogWarning("Error al enviar voto: {StatusCode} - {Response}", 
                    response.StatusCode, responseJson);
                throw new HttpRequestException($"Error al enviar voto: {response.StatusCode}");
            }

            var result = new VoteResultDto
            {
                VoteId = vote.Id,
                Message = "Voto registrado exitosamente",
                NewAverage = 0
            };

            activity?.SetTag("vote.id", result.VoteId.ToString());
            activity?.SetStatus(ActivityStatusCode.Ok);
            _logger.LogInformation("Voto registrado exitosamente: {VoteId}", result.VoteId);

            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            _logger.LogError(ex, "Error al enviar voto para producto {ProductId}", vote.ProductoId);
            throw;
        }
    }
}