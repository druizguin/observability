using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using ILogger = Microsoft.Extensions.Logging.ILogger;

public class OidcCredentials
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string GrantType { get; set; } = "password";
    public string Uri { get; set; } = string.Empty;
}

public class OidcClientCredentials : OidcCredentials
{
    public OidcClientCredentials()
    {
        GrantType = "client_credentials";
    }
}

public class OidcUserCredentials: OidcCredentials
{
    public OidcUserCredentials()
    {
        GrantType = "password";
    }
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}



public class OidcAuthService : IOidcAuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OidcCredentials _credentials;
    private readonly Microsoft.Extensions.Logging.ILogger<OidcAuthService> log;

    private System.Threading.Timer _tokenTimer = null;
    private OidcCredentialsResponse? _authToken = null;

    public OidcAuthService(
        Microsoft.Extensions.Logging.ILogger<OidcAuthService> logger,
        IHttpClientFactory httpClientFactory,
        IOptions<OidcCredentials> options)
    {
        log = logger;
        ArgumentNullException.ThrowIfNull(httpClientFactory, nameof(httpClientFactory));
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        ArgumentNullException.ThrowIfNull(options.Value, nameof(options.Value));
        _httpClientFactory = httpClientFactory;
        _credentials = options.Value;

        ArgumentNullException.ThrowIfNullOrWhiteSpace(_credentials.ClientId, "clientId");
        ArgumentNullException.ThrowIfNullOrWhiteSpace(_credentials.GrantType, "GrantType");
        ArgumentNullException.ThrowIfNullOrWhiteSpace(_credentials.ClientSecret, "clientSecret");


        if (_credentials is OidcUserCredentials uc)
        {
            _credentialsId = uc.UserName;
            ArgumentNullException.ThrowIfNullOrWhiteSpace(uc.UserName, "UserName");
            ArgumentNullException.ThrowIfNullOrWhiteSpace(uc.Password, "Password");
        }
        else
        {
            _credentialsId = _credentials.ClientId + " " + _credentials.GrantType;
        }
    }

    private string _credentialsId { get; }

    public async Task<OidcCredentialsResponse?> GetTokenAsync()
    {
        if (_authToken != null) return await Task.FromResult(_authToken);

        if (_tokenTimer != null)
        {
            _tokenTimer.Dispose();
            _tokenTimer = null;
        }

        log.LogInformation($"[OidcAuthService] Gettings credencials from OIDC: {_credentials.Uri}, id={_credentialsId}");
        var client = _httpClientFactory.CreateClient();

        var formData = new Dictionary<string, string>
            {
                { "grant_type", _credentials.GrantType },
                { "client_id", _credentials.ClientId },
                { "client_secret", _credentials.ClientSecret }
            };

        if (_credentials is OidcUserCredentials uc)
        {
            formData.Add("username", uc.UserName);
            formData.Add("password", uc.Password);
        }

        var content = new FormUrlEncodedContent(formData);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");

        var response = await client.PostAsync(_credentials.Uri, content);

        if (!response.IsSuccessStatusCode)
        {
            log.LogError($"Error in oidc response: {response.StatusCode} - {response.Content.ReadAsStringAsync().Result}");
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var token = doc.RootElement.GetProperty("access_token").GetString();
        var expires = doc.RootElement.GetProperty("expires_in").GetInt32();
        var type = doc.RootElement.GetProperty("token_type").GetString();
        var id = doc.RootElement.GetProperty("id_token").GetString();
        var result = OidcCredentialsResponse.Factory(token, expires, type, id);

        log.LogInformation($"[OidcAuthService] token ready: {_credentials.Uri}, id={_credentialsId}. Expires={result.ExpiresIn}");

        var seconds = result.ExpiresIn - 10;
        if (_tokenTimer == null)
        {
            _tokenTimer = new System.Threading.Timer(_ => RefreshAuthToken(), null, TimeSpan.FromSeconds(seconds), TimeSpan.Zero);
        }
        else
        {
            _tokenTimer.Change(TimeSpan.FromSeconds(seconds), TimeSpan.Zero);
        }

        _authToken = result;

        return result;
    }

    private void RefreshAuthToken()
    {
        _authToken = null;
        GetTokenAsync().GetAwaiter().GetResult();
    }

}