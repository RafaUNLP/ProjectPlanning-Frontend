using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using backend.DTOs;

namespace backend.Services;
public class Access
{
    private readonly HttpClient _client;
    private readonly CookieContainer _cookieContainer = new CookieContainer();

    public Access()
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = _cookieContainer,
            UseCookies = true,
            AllowAutoRedirect = false
        };
        _client = new HttpClient(handler);
        //_client.BaseAddress = new Uri("http://host.docker.internal:49828/bonita/");   //para Mac y Windows
        _client.BaseAddress = new Uri("http://172.17.0.1:49828/bonita/");   //para Linux
    }

    public async Task<BonitaSession?> LoginAsync(string username, string password)
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
            var bonitaToken = cookies["X-Bonita-API-Token"]?.Value;
            var jSessionId = cookies["JSESSIONID"]?.Value;

            if (!string.IsNullOrEmpty(jSessionId) && !string.IsNullOrEmpty(bonitaToken))
            {
                Console.WriteLine($"Token API obtenido para {username}");
                return new BonitaSession 
                { 
                    JSessionId = jSessionId, 
                    BonitaToken = bonitaToken
                };
            }
        }

        return null;
    }

}
