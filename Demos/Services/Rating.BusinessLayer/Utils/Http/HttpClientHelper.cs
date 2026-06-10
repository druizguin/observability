namespace Arky.Utils.Http
{
    public static class  HttpClientHelper
    {
        public static HttpClient Factory()
        {
            var httpClient = new HttpClient(UnsecureHandler());
            httpClient.Timeout = TimeSpan.FromSeconds(60);
            return httpClient;
        }

        public static HttpClientHandler UnsecureHandler()
        {
            var handler = new HttpClientHandler();
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            handler.ServerCertificateCustomValidationCallback =
                (httpRequestMessage, cert, cetChain, policyErrors) =>
                {
                    return true;
                };
            var httpClient = new HttpClient(handler);
            return handler;
        }
    }
}