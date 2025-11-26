using Microsoft.AspNetCore.Mvc;
using backend.DTOs;
using backend.Repositories;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Text;
using backend.Models;

namespace backend.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class EstadisticasController : ControllerBase
{
    private readonly ProyectoRepository _proyectoRepository;
    private readonly EtapaRepository _etapaRepository;
    private readonly BonitaService _bonitaService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public EstadisticasController(
        ProyectoRepository proyectoRepository,
        EtapaRepository etapaRepository,
        BonitaService bonitaService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _proyectoRepository = proyectoRepository;
        _etapaRepository = etapaRepository;
        _bonitaService = bonitaService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> GetEstadisticas()
    {
        try
        {

            var cloudTask = ObtenerColaboracionesDelCloud();
            var bonitaUsersTask = _bonitaService.GetUsersAsync(0, 500); // Traemos hasta 500 usuarios para mapear nombres
            var proyectosLocalesTask = _proyectoRepository.GetAllAsyncWithIncludes(includes: "Etapas");

            await Task.WhenAll(cloudTask, bonitaUsersTask, proyectosLocalesTask);

            var colaboracionesCloud = cloudTask.Result;
            var usuariosBonita = bonitaUsersTask.Result;
            var proyectosLocales = proyectosLocalesTask.Result;

            var mapOngNombres = usuariosBonita.ToDictionary(
                u => long.Parse(u.id), 
                u => u.userName
            );

            
            var topOngsRealizadas = colaboracionesCloud
                .Where(c => c.FechaRealizacion != null) // Solo las completadas
                .GroupBy(c => c.OrganizacionComprometidaId)
                .Select(g => new TopOngDTO
                {
                    OrganizacionId = g.Key,
                    Nombre = mapOngNombres.ContainsKey(g.Key) ? mapOngNombres[g.Key] : $"ONG #{g.Key}",
                    CantidadColaboraciones = g.Count()
                })
                .OrderByDescending(t => t.CantidadColaboraciones)
                .Take(3)
                .ToList();

            
            var categoriaTopGroup = colaboracionesCloud
                .GroupBy(c => c.CategoriaColaboracion)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            CategoriaMasPedidaDTO categoriaStat = new CategoriaMasPedidaDTO();

            if (categoriaTopGroup != null)
            {
                var categoriaEnum = categoriaTopGroup.Key;
                
                var topOngsCategoria = categoriaTopGroup
                    .Where(c => c.OrganizacionComprometidaId != 0) // Que haya una ONG asignada
                    .GroupBy(c => c.OrganizacionComprometidaId)
                    .Select(g => new TopOngDTO
                    {
                        OrganizacionId = g.Key,
                        Nombre = mapOngNombres.ContainsKey(g.Key) ? mapOngNombres[g.Key] : $"ONG #{g.Key}",
                        CantidadColaboraciones = g.Count()
                    })
                    .OrderByDescending(t => t.CantidadColaboraciones)
                    .Take(3)
                    .ToList();

                categoriaStat = new CategoriaMasPedidaDTO
                {
                    NombreCategoria = categoriaEnum.ToString(),
                    CantidadPedidos = categoriaTopGroup.Count(),
                    Top3OngsComprometidas = topOngsCategoria
                };
            }
            
            double promedioDias = 0;
            if (proyectosLocales.Any())
            {
                var proyectosConEtapas = proyectosLocales.Where(p => p.Etapas.Any()).ToList();
                
                if (proyectosConEtapas.Any())
                {
                    var duraciones = proyectosConEtapas.Select(p => 
                    {
                        var inicio = p.Etapas.Min(e => e.FechaInicio);
                        var fin = p.Etapas.Max(e => e.FechaFin);
                        return (fin - inicio).TotalDays;
                    });
                    
                    promedioDias = duraciones.Average();
                }
            }

            var response = new EstadisticasResponseDTO
            {
                Top3OngsColaboradoras = topOngsRealizadas,
                CategoriaMasPedida = categoriaStat,
                TiempoPromedioDias = Math.Round(promedioDias, 2),
            };

            return Ok(response);

        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error generando estadísticas: {ex.Message}");
        }
    }

    private async Task<List<ColaboracionDTO>> ObtenerColaboracionesDelCloud()
    {
        var client = _httpClientFactory.CreateClient(); 
        client.BaseAddress = new Uri("https://proyectplanning-cloud.onrender.com/");

        var loginData = new CloudLoginDTO { nombre = "walter.bates", contraseña = "bpm" };
        var jsonLogin = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");

        var loginResponse = await client.PostAsync("Login", jsonLogin);
        if (!loginResponse.IsSuccessStatusCode)
        {
            throw new Exception("Fallo el login con el Cloud API");
        }

        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var token = await loginResponse.Content.ReadAsStringAsync();
        token = token.Trim().Trim('"');
        if (string.IsNullOrEmpty(token))
        {
            throw new Exception("El Cloud API no devolvió un token válido");
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var colabResponse = await client.GetAsync("Colaboracion");
        if (!colabResponse.IsSuccessStatusCode)
        {
             Console.WriteLine($"Error obteniendo colaboraciones: {colabResponse.StatusCode}");
             return new List<ColaboracionDTO>();
        }

        var colabContent = await colabResponse.Content.ReadAsStringAsync();
        
        var colaboraciones = JsonSerializer.Deserialize<List<ColaboracionDTO>>(colabContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        return colaboraciones ?? new List<ColaboracionDTO>();
    }
}