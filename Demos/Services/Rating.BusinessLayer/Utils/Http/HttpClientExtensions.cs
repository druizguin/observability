using Microsoft.Extensions.Logging;

namespace System.Net.Http;

public static class HttpClientExtensions
{
    public static void EnsureResponse(this HttpResponseMessage response, string provider = "HTTPClient")
    {
        ArgumentNullException.ThrowIfNull(response, nameof(response));

        //si la responesta es errónea extraer el error
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            throw new HttpRequestException($"Error {provider}: StatusCode={response.StatusCode} ErrorContent={errorContent}");
        }
    }

    public static async Task<bool> EnsureResponseAsync(
        this HttpResponseMessage response, 
        ILogger logger, 
        string provider = "HTTPClient")
    {
        ArgumentNullException.ThrowIfNull(response, nameof(response));

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            logger.LogError($"Error {provider}: StatusCode={response.StatusCode} ErrorContent={errorContent}");
            return false;
        }

        return true;
    }
}


