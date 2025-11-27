using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.DTOs;
using backend.Repositories;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Collections.Immutable;
using Microsoft.Extensions.Caching.Memory;

namespace backend.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ProyectoController : ControllerBase
{
    private readonly ProyectoRepository _proyectoRepository;
    private readonly PropuestaColaboracionRepository _propuestaRepository;
    private readonly EtapaRepository _etapaRepository;
    private readonly BonitaService _bonitaService;
    public ProyectoController(ProyectoRepository proyectoRepository, PropuestaColaboracionRepository propuestaRepository, EtapaRepository etapaRepository , BonitaService bonitaService)
    {
        _proyectoRepository = proyectoRepository;
        _propuestaRepository = propuestaRepository;
        _etapaRepository = etapaRepository;
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
                    Completada = !e.RequiereColaboracion,
                    CategoriaColaboracion = e.CategoriaColaboracion,
                    DescripcionColaboracion = e.DescripcionColaboracion
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

    /// <summary>
    /// Recupera los proyectos que aún esperan colaboraciones junto con sus etapas que NO tienen propuestas aún.
    /// NOTA: te da hasta las de tus propios proyectos, así que el front lo filtra por organizacionId
    /// </summary>
    [HttpGet("requierenColaboraciones")]
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
                    var existePropuestaAceptada = await _propuestaRepository.Exist(prop => prop.EtapaId == etapa.Id && prop.Estado == EstadoPropuestaColaboracion.Aceptada);
                    if (!existePropuestaAceptada)
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

    /// <summary>
    /// Recupera los proyectos de una organizacion con sus etapas y propuestas .
    /// </summary>
    /// <param name="userId">ID en Bonita del usuario</param>
    [HttpGet("porOrganizacion/{userId}")]
    public async Task<IActionResult> RecuperarProyectosPorOrganizacionId(long userId)
    {
        try
        {
            // Obtener proyectos
            IEnumerable<Proyecto> proyectos = await _proyectoRepository.FilterAsync(
                p => p.OrganizacionId == userId, 
                orderBy: order => order.OrderByDescending(p => p.Fecha),
                includes: "Etapas"
            );

            // Listado de proyectos
            var listado = new List<ListarProyectosDTO>();

            foreach (var p in proyectos)
            {
                // Crear DTO para cada proyecto
                var proyectoDto = new ListarProyectosDTO
                {
                    Id = p.Id,
                    Descripcion = p.Descripcion,
                    Nombre = p.Nombre,
                    OrganizacionId = p.OrganizacionId,
                    Completado = p.Completado,
                    Etapas = new List<EtapaConPropuestasDTO>()
                };

                // Traer etapas y sus propuestas de manera secuencial
                foreach (var e in p.Etapas)
                {
                    var etapaDto = new EtapaConPropuestasDTO
                    {
                        Id = e.Id,
                        Nombre = e.Nombre,
                        RequiereColaboracion = e.RequiereColaboracion,
                        Descripcion = e.Descripcion,
                        FechaInicio = e.FechaInicio,
                        FechaFin = e.FechaFin,
                        Completada = e.Completada,
                        Propuestas = await _propuestaRepository.FilterAsync(prop => prop.EtapaId == e.Id)
                    };

                    proyectoDto.Etapas.Add(etapaDto);
                }

                listado.Add(proyectoDto);
            }

            return Ok(listado);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }


    /// <summary>
    /// Avanza la actividad "Completar etapa" en Bonita para una etapa específica.
    /// </summary>
    /// <param name="etapaId">ID de la etapa a completar</param>
    [HttpPost("completar-etapa/{etapaId}")]
    public async Task<IActionResult> CompletarEtapa(Guid etapaId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("No se pudo identificar al usuario a partir del token JWT.");
            }

            var etapa = await _etapaRepository.GetAsync(etapaId);
            if (etapa == null)
            {
                return NotFound($"No se encontró la etapa con ID: {etapaId}");
            }

            if (etapa.ColaboracionId == null)
            {
                return BadRequest("La etapa indicada no tiene una colaboración asociada (ColaboracionId es null), por lo que no se puede enviar a Bonita.");
            }

            var proyecto = await _proyectoRepository.GetAsync(etapa.ProyectoId);
            if (proyecto == null)
            {
                return NotFound("No se encontró el proyecto asociado a esta etapa.");
            }
            
            long caseId = proyecto.BonitaCaseId;

            try
            {
                var activity = await _bonitaService.GetActivityByCaseIdAndDisplayName(caseId.ToString(), "Completar etapa");

                await _bonitaService.AssignActivityToUser(activity.id, userId);

                
                await _bonitaService.SetVariableByCase(
                    caseId.ToString(), 
                    "colaboracionCompletarId", 
                    etapa.ColaboracionId.ToString(), 
                    "java.lang.String"
                );

                bool finished = await _bonitaService.CompleteActivityAsync(activity.id);

                if (!finished)
                {                    
                    return StatusCode(502, "Falló la terminación de la tarea en Bonita.");
                }
                
                etapa.Completada = true;
                await _etapaRepository.UpdateAsync(etapa, etapa);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(502, $"Error en la comunicación con Bonita: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
    
    // <summary>
    /// Verifica que todas las etapas estén listas y completa el proyecto en Bonita.
    /// </summary>
    /// <param name="proyectoId">ID del proyecto a completar.</param>
    [HttpPost("completar-proyecto/{proyectoId}")]
    public async Task<IActionResult> CompletarProyecto(Guid proyectoId)
    {
        try
        {
            var proyecto = await _proyectoRepository.GetAsync(proyectoId);
            if (proyecto == null)
            {
                return NotFound($"No se encontró el proyecto con ID: {proyectoId}");
            }

            var etapas = await _etapaRepository.FilterAsync(e => e.ProyectoId == proyectoId);

            var etapasIncompletas = etapas.Where(e => !e.Completada).ToList();

            if (etapasIncompletas.Any())
            {
                var nombres = string.Join(", ", etapasIncompletas.Select(e => e.Nombre));
                return BadRequest($"No se puede completar el proyecto. Las siguientes etapas aún no están completadas: {nombres}");
            }

            // (Opcional) También podrías validar que las etapas que NO requieren colaboración 
            // también estén marcadas como completadas, si esa fuera tu lógica.
            // Por ahora, nos ceñimos estrictamente a tu requerimiento.

            // 3. Interactuar con Bonita
            try
            {
                long caseId = proyecto.BonitaCaseId;

                // a. Validar usuario
                var userIdBonita = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdBonita))
                {
                    return Unauthorized("No se pudo identificar al usuario a partir del token JWT.");
                }

                // b. Obtener la actividad "Completar proyecto"
                // Busca la actividad por nombre en el caso correspondiente
               

                var activity= await _bonitaService.GetActivityByCaseIdAndDisplayName(caseId.ToString(), "Completar proyecto");

                // c. Asignar y Completar
                await _bonitaService.AssignActivityToUser(activity.id, userIdBonita);
                
                bool finished = await _bonitaService.CompleteActivityAsync(activity.id);

                if (!finished)
                {
                    return StatusCode(502, "Falló la terminación de la tarea 'Completar proyecto' en Bonita.");
                }
                proyecto.Completado = true;
                await _proyectoRepository.UpdateAsync(proyecto, proyecto);                  

                return Ok(new { message = "Proyecto completado exitosamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(502, $"Error en la comunicación con Bonita: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}