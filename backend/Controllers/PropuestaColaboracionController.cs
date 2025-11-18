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
public class PropuestaColaboracionController : ControllerBase
{
    private readonly BonitaService _bonitaService;
    private readonly PropuestaColaboracionRepository _propuestaRepository;
    private readonly EtapaRepository _etapaRepository;
    private readonly ProyectoRepository _proyectoRepository;

    public PropuestaColaboracionController(
        BonitaService bonitaService,
        PropuestaColaboracionRepository propuestaRepository,
        EtapaRepository etapaRepository,
        ProyectoRepository proyectoRepository)
    {
        _bonitaService = bonitaService;
        _propuestaRepository = propuestaRepository;
        _etapaRepository = etapaRepository;
        _proyectoRepository = proyectoRepository;
    }

    [HttpPost]
    public async Task<IActionResult> CrearPropuesta([FromBody] PropuestaColaboracionDTO propuestaDTO)
    {
        try
        {
            Etapa? etapa = await _etapaRepository.GetAsync(propuestaDTO.EtapaId);
            if (etapa == null)
            {
                return NotFound($"No se encontró la etapa con ID: {propuestaDTO.EtapaId}");
            }

            if (!etapa.RequiereColaboracion)
            {
                return BadRequest("La etapa seleccionada no requiere colaboración, por lo que no se pueden proponer compromisos para la misma.");
            }

            Proyecto? proyecto = await _proyectoRepository.GetAsync(etapa.ProyectoId);
            if (proyecto == null)
            {
                return NotFound($"No se encontró el proyecto asociado a la etapa.");
            }

            long caseId = proyecto.BonitaCaseId;

            BonitaActivityResponse activity;
            try
            {
                activity = await _bonitaService.GetActivityByCaseIdAndName(caseId.ToString(), "Proponer compromiso con una etapa");

                var userName = User.FindFirst(ClaimTypes.NameIdentifier).Value;
                if (string.IsNullOrEmpty(userName))
                {
                    return Unauthorized("No se pudo identificar al usuario a partir del token JWT.");
                }

                var userId = await _bonitaService.GetUserIdByUserName(userName);
                await _bonitaService.AssignActivityToUser(activity.id, userId);
            }
            catch (Exception ex)
            {
                return StatusCode(502, $"Error en la integración con Bonita: {ex.Message}");
            }

            // 5. GUARDAR LA PROPUESTA EN LA BD LOCAL
            PropuestaColaboracion nuevaPropuesta = new PropuestaColaboracion()
            {
                Id = Guid.NewGuid(),
                EtapaId = propuestaDTO.EtapaId,
                OrganizacionProponenteId = propuestaDTO.OrganizacionProponenteId,
                Descripcion = propuestaDTO.Descripcion,
                CategoriaColaboracion = propuestaDTO.CategoriaColaboracion,
                EsParcial = propuestaDTO.EsParcial
            };
            
            await _propuestaRepository.AddAsync(nuevaPropuesta);

            // 6. COMPLETAR LA TAREA EN BONITA
            bool finishedActivity = await _bonitaService.CompleteActivityAsync(activity.id);

            if (finishedActivity)
                return Ok(nuevaPropuesta);
            else
                return StatusCode(502, "Falló la terminación de la actividad 'Proponer compromiso' en Bonita");
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>
    /// Obtiene todas las Propuestas de Colaboración asociadas a un Proyecto específico.
    /// </summary>
    /// <param name="proyectoId">El Guid del Proyecto del cual se quieren ver las propuestas.</param>
    /// <returns>Una lista de Propuestas de Colaboración.</returns>
    [HttpGet("proyecto/{proyectoId}")]
    public async Task<IActionResult> GetPropuestasPorProyecto(Guid proyectoId)
    {
        try
        {
            // 1. Validar que el proyecto existe (buena práctica)
            bool proyectoExiste = await _proyectoRepository.Exist(p => p.Id == proyectoId);
            if (!proyectoExiste)
            {
                return NotFound($"No se encontró el proyecto con ID: {proyectoId}");
            }

            // 2. Usar el repositorio de Propuestas para filtrar
            //    Esto mágicamente hace un JOIN con la tabla Etapa y filtra por ProyectoId.
            var propuestas = await _propuestaRepository.FilterAsync(
                filtro: p => p.Etapa.ProyectoId == proyectoId,
                includes: "Etapa,OrganizacionProponente" // Incluimos datos de la Etapa y la Org
            );

            // 3. Devolver la lista de propuestas
            return Ok(propuestas);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}