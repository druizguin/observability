public interface IOidcAuthService
{
    Task<OidcCredentialsResponse> GetTokenAsync();
}
