using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.DTOs;
using backend.Repositories;
using backend.Services;
using Microsoft.VisualBasic;

namespace backend.Controllers;

[ApiController]
[Route("[controller]")]
public class ProyectoController : ControllerBase
{
    private readonly ProyectoRepository _proyectoRepository;
    private readonly BonitaService _bonitaService;
    public ProyectoController(ProyectoRepository proyectoRepository)
    {
        _proyectoRepository = proyectoRepository;
        var _access = new Access();
        var RequestHelper = _access.LoginAsync("walter.bates", "bpm").Result;
        if (RequestHelper != null)
        {
            _bonitaService = new BonitaService(RequestHelper);            
        }
    }

    [HttpPost]
    public async Task<IActionResult> CrearProyecto(ProyectoDTO proyectoDTO)
    {
        try
        {
            if (proyectoDTO.Etapas.Any(e => e.FechaFin <= e.FechaInicio))
                return BadRequest("La fecha de fin de todas las etapas debe ser mayor a la fecha de inicio de la misma.");

            Proyecto nuevo = await _proyectoRepository.AddAsync(new Proyecto()
            {
                Id = Guid.NewGuid(),
                Nombre = proyectoDTO.Nombre,
                Descripcion = proyectoDTO.Descripcion,
                Fecha = DateTime.Now,
                Etapas = proyectoDTO.Etapas.Select(e => new Etapa()
                {
                    Id = Guid.NewGuid(),
                    Nombre = e.Nombre,
                    Descripcion = e.Descripcion,
                    FechaInicio = e.FechaInicio.ToLocalTime(),
                    FechaFin = e.FechaFin.ToLocalTime(),
                    Colaboracion = (e.CategoriaColaboracion == null) ? null : new Colaboracion()
                    {
                        Id = Guid.NewGuid(),
                        CategoriaColaboracion = e.CategoriaColaboracion.Value,
                        Descripcion = e.DescripcionColaboracion ?? string.Empty,
                        EtapaId = e.Id ?? Guid.Empty
                    }
                }).ToList()
            });

            var idProc = await _bonitaService.GetProcessIdByName("Prueba1");//recupera id del proceso
            var caseId = await _bonitaService.StartProcessById(idProc);//inicia una instancia del mismo
            var suc = await _bonitaService.SetVariableByCase(caseId.ToString(), "var1", "valor1", "java.lang.String");//le instancia variables de prueba
            var activity = await _bonitaService.GetActivityByCaseId(caseId.ToString());//recupera el id de la actividad
            Console.WriteLine($"Actividad: {activity}");
            var userId = await _bonitaService.GetUserIdByUserName("walter.bates");//hay que asignar un usuario a la actividad para completarla, recupera el id usuario en bonita
            await _bonitaService.AssignActivityToUser(activity.id, userId);//le asigna la actividad al usuario
            bool finishedActivity = await _bonitaService.CompleteActivityAsync(activity.id);//completa la actividad

            if (finishedActivity)
                return Ok(nuevo);
            else
                return StatusCode(502,"Falló la terminación de la actividad en Bonita");
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> RecuperarProyecto(Guid id)
    {
        try
        {
            Proyecto? buscado = await _proyectoRepository.GetAsyncWithIncludes(id, includes: "Etapas");

            if (buscado == null)
                return NotFound();

            return Ok(buscado);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}