using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

public class Access
{
    private readonly HttpClient _client;
    private readonly CookieContainer _cookieContainer = new CookieContainer();
    private string _bonitaToken;

    public Access()
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = _cookieContainer,
            UseCookies = true,
            AllowAutoRedirect = false
        };
        _client = new HttpClient(handler);
        _client.BaseAddress = new Uri("http://localhost:49828/bonita/");
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("username", username),
            new KeyValuePair<string, string>("password", password),
            new KeyValuePair<string, string>("redirect", "false")
        });

        var response = await _client.PostAsync("loginservice", content);
        if (response.IsSuccessStatusCode)
        {
            // Extraer cookie X-Bonita-API-Token
            var cookies = _cookieContainer.GetCookies(_client.BaseAddress);
            _bonitaToken = cookies["X-Bonita-API-Token"]?.Value;

            Console.WriteLine($"Token obtenido: {_bonitaToken}");
            return !string.IsNullOrEmpty(_bonitaToken);
        }

        return false;
    }

    public async Task<string> GetAsync(string endpoint)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        if (!string.IsNullOrEmpty(_bonitaToken))
        {
            request.Headers.Add("X-Bonita-API-Token", _bonitaToken);
        }

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
