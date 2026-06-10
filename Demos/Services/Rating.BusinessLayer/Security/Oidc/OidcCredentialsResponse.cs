public class OidcCredentialsResponse : IOidcCredentialsResponse
{
    public string Token { get; set; } = string.Empty;

    public string IdToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; } = 0; // seconds
    public required string Type { get; set; }

    public static OidcCredentialsResponse Factory(string token, int expires, string type, string id)
    {
        return new OidcCredentialsResponse
        {
            Token = token,
            ExpiresIn = expires,
            Type = type,
            IdToken = id
        };
    }
}
