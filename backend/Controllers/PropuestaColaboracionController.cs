using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.DTOs;
using backend.Repositories;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;

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
    // private readonly ColaboracionRepository _colaboracionRepository;

    public PropuestaColaboracionController(
        BonitaService bonitaService,
        PropuestaColaboracionRepository propuestaRepository,
        EtapaRepository etapaRepository,
        ProyectoRepository proyectoRepository)
        //ColaboracionRepository colaboracionRepository)
    {
        _bonitaService = bonitaService;
        _propuestaRepository = propuestaRepository;
        _etapaRepository = etapaRepository;
        _proyectoRepository = proyectoRepository;
        //_colaboracionRepository = colaboracionRepository;
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
            
            bool yaExiste = await _propuestaRepository.Exist(p => p.EtapaId == propuestaDTO.EtapaId && p.Estado == EstadoPropuestaColaboracion.Aceptada);
            if (yaExiste)
            {
                return Conflict("La etapa ya tiene una colaboración aceptada, no se pueden proponer más compromisos.");
            }

            long caseId = proyecto.BonitaCaseId;

            BonitaActivityResponse activity;
            try
            {
                activity = await _bonitaService.GetActivityByCaseIdAndName(caseId.ToString(), "Proponer compromiso con una etapa");
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
            bool proyectoExiste = await _proyectoRepository.Exist(p => p.Id == proyectoId);
            if (!proyectoExiste)
            {
                return NotFound($"No se encontró el proyecto con ID: {proyectoId}");
            }

            var propuestas = await _propuestaRepository.FilterAsync(
                filtro: p => p.Etapa.ProyectoId == proyectoId,
                includes: "Etapa" 
            );

            return Ok(propuestas);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }


    /// <summary>
    /// Acepta una Propuesta de Colaboración, creando una Colaboracion
    /// y avanzando el proceso en Bonita.
    /// </summary>
    /// <param name="propuestaId">El Guid de la PropuestaColaboracion a aceptar.</param>
    /// <returns>La nueva Colaboracion que se ha creado (en memoria).</returns>
    [HttpPost("aceptar/{propuestaId}")]
    public async Task<IActionResult> AceptarPropuesta(Guid propuestaId)
    {
        try
        {
            //voy a tener que ver si ya existe una propuesta aceptada para la misma etapa y organización comprometida
            PropuestaColaboracion? propuesta = await _propuestaRepository.GetAsyncWithIncludes(propuestaId, "Etapa");
            if (propuesta == null)
            {
                return NotFound($"No se encontró la propuesta con ID: {propuestaId}");
            }
            if (propuesta.Etapa == null)
            {
                return NotFound("La propuesta no está asociada a ninguna etapa.");
            }

            Proyecto? proyecto = await _proyectoRepository.GetAsync(propuesta.Etapa.ProyectoId);
            if (proyecto == null)
            {
                return NotFound($"No se encontró el proyecto asociado a la propuesta.");
            }

            bool yaExiste = await _propuestaRepository.Exist(p => p.EtapaId == propuesta.EtapaId && p.Estado == EstadoPropuestaColaboracion.Aceptada);
            if (yaExiste)
            {
                return Conflict("Ya existe una colaboración aceptada para la etapa asociada a esta propuesta.");
            }
            
            long caseId = proyecto.BonitaCaseId;


            var colaboracionPayload = new CrearColaboracionDTO
            {
                Proyecto = proyecto.Nombre,
                Descripcion = propuesta.Descripcion,
                CategoriaColaboracion = propuesta.CategoriaColaboracion,
                ProyectoId = proyecto.Id,
                EtapaId = propuesta.EtapaId,
                OrganizacionProyectoId = proyecto.OrganizacionId, 
                OrganizacionComprometidaId = propuesta.OrganizacionProponenteId,                 
                FechaRealizacion = null 
            };

            BonitaActivityResponse activity;
            try
            {
                activity = await _bonitaService.GetActivityByCaseIdAndDisplayName(caseId.ToString(), "Evaluar propuestas de colaboración");                
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("No se pudo identificar al usuario a partir del token JWT.");
                }
                await _bonitaService.AssignActivityToUser(activity.id, userId);

                var colaboracionJson = JsonSerializer.Serialize(colaboracionPayload);
                await _bonitaService.SetVariableByCase(caseId.ToString(), "colaboracionIn", colaboracionJson, "java.lang.String");

                
            }
            catch (Exception ex)
            {
                return StatusCode(502, $"Error en la integración con Bonita: {ex.Message}");
            }

            propuesta.Estado = EstadoPropuestaColaboracion.Aceptada;
            await _propuestaRepository.UpdateAsync(propuesta, propuesta);
            bool finishedActivity = await _bonitaService.CompleteActivityAsync(activity.id);

            if (!finishedActivity)
            {
                return StatusCode(502, "Falló la terminación de la actividad 'Evaluar propuestas' en Bonita.");
            }
            var colaboracionOut = await _bonitaService.GetVariableByCaseIdAndName(caseId, "colaboracionOut");
            return Ok(colaboracionOut);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}