using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace backend.Services;

public class RequestHelper
{
    private readonly HttpClient _client;
    private readonly string _bonitaApiToken;
    private readonly string _bonitaJSessionId;

    public RequestHelper(HttpClient client, string bonitaApiToken, string bonitaJSessionId)
    {
        _client = client;
        _bonitaApiToken = bonitaApiToken;
        _bonitaJSessionId = bonitaJSessionId;
    }

    public async Task<T> DoRequestAsync<T>(HttpMethod method, string endpoint, HttpContent content = null)
    {
        var request = new HttpRequestMessage(method, endpoint);

        if (!string.IsNullOrEmpty(_bonitaApiToken) && !string.IsNullOrEmpty(_bonitaJSessionId))
        {
            request.Headers.Add("Cookie", $"X-Bonita-API-Token={_bonitaApiToken}; JSESSIONID={_bonitaJSessionId}");
        }

        if (method != HttpMethod.Get && !string.IsNullOrEmpty(_bonitaApiToken))
        {
            request.Headers.Add("X-Bonita-API-Token", _bonitaApiToken);
        }

        if (content != null)
            request.Content = content;

        var response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Respuesta no exitosa de Bonita: {(int)response.StatusCode} {response.ReasonPhrase}. Content: {errorContent}",
                null,
                response.StatusCode);
        }

        
        var responseContent = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(responseContent))
        {
            if (typeof(T) == typeof(string))
                return (T)(object)string.Empty;

            return default;
        }

        if (typeof(T) == typeof(string))
            return (T)(object)responseContent;

        // Para otros tipos, intentamos deserializar JSON
        return JsonSerializer.Deserialize<T>(responseContent);
    }

}
