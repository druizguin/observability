public interface IOidcCredentialsResponse
{
    int ExpiresIn { get; set; }
    string IdToken { get; set; }
    string Token { get; set; }
    string Type { get; set; }

    static abstract OidcCredentialsResponse Factory(string token, int expires, string type, string id);
}