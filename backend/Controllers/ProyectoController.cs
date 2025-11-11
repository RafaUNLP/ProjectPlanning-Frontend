using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.DTOs;
using backend.Repositories;
using backend.Services;
using Microsoft.VisualBasic;
using Microsoft.AspNetCore.Authorization;

namespace backend.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ProyectoController : ControllerBase
{
    private readonly ProyectoRepository _proyectoRepository;
    private readonly BonitaService _bonitaService;
    public ProyectoController(ProyectoRepository proyectoRepository, BonitaService bonitaService)
    {
        _proyectoRepository = proyectoRepository;
        _bonitaService = bonitaService;
    }

    [HttpPost]
    public async Task<IActionResult> CrearProyecto(ProyectoDTO proyectoDTO)
    {
        try
        {
            bool nombreProyectoEnUso = await _proyectoRepository.Exist(p => p.Nombre.ToLower() == proyectoDTO.Nombre.ToLower());

            if (nombreProyectoEnUso)
                return Conflict("El nombre de proyecto ya está en uso, por favor use uno distinto.");

            if (proyectoDTO.Etapas.Any(e => e.FechaFin <= e.FechaInicio))
                return BadRequest("La fecha de fin de todas las etapas debe ser mayor a la fecha de inicio de la misma.");

            BonitaActivityResponse activity;
            try
            {
                var idProc = await _bonitaService.GetProcessIdByName("Proceso de realización de un proyecto");
                var caseId = await _bonitaService.StartProcessById(idProc);
                var proyectoJson = System.Text.Json.JsonSerializer.Serialize(proyectoDTO);
                var success = await _bonitaService.SetVariableByCase(caseId.ToString(), "proyecto", proyectoJson, "java.lang.String");
                activity = await _bonitaService.GetActivityByCaseIdAndName(caseId.ToString(), "Cargar el proyecto");
                Console.WriteLine(activity);
                Console.WriteLine($"Actividad: {activity.name} - {activity.id}");
                var userId = await _bonitaService.GetUserIdByUserName("walter.bates");
                await _bonitaService.AssignActivityToUser(activity.id, userId);
            }
            catch (Exception ex)
            {
                return StatusCode(502, $"Error en la integración con Bonita: {ex.Message}");
            }

            Proyecto nuevo = await _proyectoRepository.AddAsync(new Proyecto()
            {
                Id = Guid.NewGuid(),
                Nombre = proyectoDTO.Nombre,
                Descripcion = proyectoDTO.Descripcion,
                OrganizacionId = proyectoDTO.OrganizacionId,
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
                        EtapaId = e.Id ?? Guid.Empty,
                    }
                }).ToList()
            });

            
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
            Proyecto? buscado = await _proyectoRepository.GetAsyncWithIncludes(id, includes: "Etapas,Etapas.Colaboracion");

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