using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;

namespace WebOpenObserveDemo.Controllers;

[ApiController]
[Route("api/telemetry")]
public class TelemetryProxyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelemetryProxyController> _logger;

    public TelemetryProxyController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<TelemetryProxyController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("v1/traces")]
    public async Task<IActionResult> ProxyTraces()
    {
        return await ProxyRequest("traces");
    }

    [HttpPost("v1/logs")]
    public async Task<IActionResult> ProxyLogs()
    {
        return await ProxyRequest("logs");
    }

    [HttpPost("v1/metrics")]
    public async Task<IActionResult> ProxyMetrics()
    {
        return await ProxyRequest("metrics");
    }

    private async Task<IActionResult> ProxyRequest(string signalType)
    {
        try
        {
            // Leer configuración desde appsettings.json
            var openObserveUrl = _configuration["OpenObserve:Endpoint"] ?? "http://localhost:5080";
            var organization = _configuration["OpenObserve:Organization"] ?? "default";
            var authToken = _configuration["OpenObserve:AuthToken"] ?? "cm9vdEBleGFtcGxlLmNvbTozdm5VZmZIbmlmTENGZEZ4";

            var targetUrl = $"{openObserveUrl}/api/{organization}/v1/{signalType}";

            // Leer el body de la petición original
            using var reader = new StreamReader(Request.Body);
            var requestBody = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                _logger.LogWarning("Received empty request body for {SignalType}", signalType);
                return BadRequest("Request body is empty");
            }

            // Crear cliente HTTP
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            // Crear la petición
            var request = new HttpRequestMessage(HttpMethod.Post, targetUrl)
            {
                Content = new StringContent(requestBody, Encoding.UTF8, Request.ContentType ?? "application/json")
            };

            // Agregar header de autorización
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);

            // Copiar headers importantes del request original
            foreach (var header in Request.Headers)
            {
                if (header.Key.StartsWith("x-", StringComparison.OrdinalIgnoreCase) ||
                    header.Key.Equals("traceparent", StringComparison.OrdinalIgnoreCase) ||
                    header.Key.Equals("tracestate", StringComparison.OrdinalIgnoreCase))
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            // Enviar petición a OpenObserve
            var response = await httpClient.SendAsync(request);

            // Leer respuesta
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "OpenObserve returned error {StatusCode} for {SignalType}: {Response}",
                    response.StatusCode,
                    signalType,
                    responseContent);
            }

            // Retornar la respuesta de OpenObserve
            return StatusCode((int)response.StatusCode, responseContent);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error proxying {SignalType} to OpenObserve", signalType);
            return StatusCode(502, new { error = "Failed to connect to OpenObserve", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying {SignalType} to OpenObserve", signalType);
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }
}