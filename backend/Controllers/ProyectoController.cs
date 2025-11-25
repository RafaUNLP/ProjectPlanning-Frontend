using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.DTOs;
using backend.Repositories;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ProyectoController : ControllerBase
{
    private readonly ProyectoRepository _proyectoRepository;
    private readonly PropuestaColaboracionRepository _propuestaRepository;
    private readonly BonitaService _bonitaService;
    public ProyectoController(ProyectoRepository proyectoRepository, PropuestaColaboracionRepository propuestaRepository, BonitaService bonitaService)
    {
        _proyectoRepository = proyectoRepository;
        _propuestaRepository = propuestaRepository;
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
            long caseId;
            try
            {
                var idProc = await _bonitaService.GetProcessIdByName("Proceso de realización de un proyecto");
                caseId = await _bonitaService.StartProcessById(idProc);
                var proyectoJson = System.Text.Json.JsonSerializer.Serialize(proyectoDTO);
                var success = await _bonitaService.SetVariableByCase(caseId.ToString(), "proyecto", proyectoJson, "java.lang.String");
                activity = await _bonitaService.GetActivityByCaseIdAndName(caseId.ToString(), "Cargar el proyecto");

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("No se pudo identificar al usuario a partir del token JWT.");
                }

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
                BonitaCaseId = caseId,
                Etapas = proyectoDTO.Etapas.Select(e => new Etapa()
                {
                    Id = Guid.NewGuid(),
                    Nombre = e.Nombre,
                    Descripcion = e.Descripcion,
                    FechaInicio = e.FechaInicio.ToLocalTime(),
                    FechaFin = e.FechaFin.ToLocalTime(),
                    RequiereColaboracion = e.RequiereColaboracion,
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

    [HttpGet("/requierenColaboraciones")]
    public async Task<IActionResult> RecuperarProyectoQueRequierenColaboraciones()
    {
        try
        {
            var proyectos = await _proyectoRepository.FilterAsync(p => p.Etapas.Any(e => e.RequiereColaboracion && e.ColaboracionId == null), includes:"Etapas");
            
            foreach (Proyecto proyecto in proyectos)
            {
                var etapasFiltradas = proyecto.Etapas.Where(e => e.RequiereColaboracion && e.ColaboracionId == null).ToList();

                // Filtrar asincrónicamente las etapas que no tienen propuestas
                var etapasConPropuestas = new List<Etapa>();
                foreach (var etapa in etapasFiltradas)
                {
                    var existePropuesta = await _propuestaRepository.Exist(prop => prop.EtapaId == etapa.Id);
                    if (!existePropuesta)
                    {
                        etapasConPropuestas.Add(etapa);
                    }
                }

                // Asigna las etapas filtradas
                proyecto.Etapas = etapasConPropuestas;
            }

            return Ok(proyectos);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}