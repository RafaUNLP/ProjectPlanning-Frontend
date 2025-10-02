using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace backend.Services;

public class RequestHelper
{
    private readonly HttpClient _client;
    private readonly string _bonitaToken;

    public RequestHelper(HttpClient client, string bonitaToken)
    {
        _client = client;
        _bonitaToken = bonitaToken;
    }

    public async Task<T> DoRequestAsync<T>(HttpMethod method, string endpoint, HttpContent content = null)
    {
        var request = new HttpRequestMessage(method, endpoint);

        if (!string.IsNullOrEmpty(_bonitaToken))
            request.Headers.Add("X-Bonita-API-Token", _bonitaToken);

        if (content != null)
            request.Content = content;

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();

        // Si la respuesta está vacía
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            // Si T es string, devolvemos ""
            if (typeof(T) == typeof(string))
                return (T)(object)string.Empty;

            // Si T es tipo referencia o nullable, devolvemos null
            return default;
        }

        // Si T es string, devolvemos el contenido crudo
        if (typeof(T) == typeof(string))
            return (T)(object)responseContent;

        // Para otros tipos, intentamos deserializar JSON
        return JsonSerializer.Deserialize<T>(responseContent);
    }

}
