using System.Net.Http;

namespace WFBC.Client
{
    public class AuthorizedClient
    {
        public HttpClient Client { get; }

        public AuthorizedClient(HttpClient httpClient)
        {
            Client = httpClient;
        }
    }
}
