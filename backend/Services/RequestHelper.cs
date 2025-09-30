using System.Net.Http;
using System.Threading.Tasks;

public class RequestHelper
{
    private readonly HttpClient _client;
    private readonly string _bonitaToken;

    public RequestHelper(HttpClient client, string bonitaToken)
    {
        _client = client;
        _bonitaToken = bonitaToken;
    }

    public async Task<string> DoRequestAsync(HttpMethod method, string endpoint, HttpContent content = null)
    {
        var request = new HttpRequestMessage(method, endpoint);

        // Agregar token como header
        if (!string.IsNullOrEmpty(_bonitaToken))
        {
            request.Headers.Add("X-Bonita-API-Token", _bonitaToken);
        }

        if (content != null)
            request.Content = content;

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}
