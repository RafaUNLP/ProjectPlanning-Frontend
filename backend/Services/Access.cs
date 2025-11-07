using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace backend.Services;
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
        _client.BaseAddress = new Uri("http://host.docker.internal:49828/bonita/");    }

    public async Task<RequestHelper> LoginAsync(string username, string password)
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
            var cookies = _cookieContainer.GetCookies(_client.BaseAddress);
            _bonitaToken = cookies["X-Bonita-API-Token"]?.Value;

            Console.WriteLine($"Token obtenido: {_bonitaToken}");
            return new RequestHelper(_client, _bonitaToken);
        }

        return null;
    }

}
