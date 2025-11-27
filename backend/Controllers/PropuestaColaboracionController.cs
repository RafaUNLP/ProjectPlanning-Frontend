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
    private readonly ObservacionRepository _observacionRepository;

    public PropuestaColaboracionController(
        BonitaService bonitaService,
        PropuestaColaboracionRepository propuestaRepository,
        EtapaRepository etapaRepository,
        ProyectoRepository proyectoRepository,
        ObservacionRepository observacionRepository)
    {
        _bonitaService = bonitaService;
        _propuestaRepository = propuestaRepository;
        _etapaRepository = etapaRepository;
        _proyectoRepository = proyectoRepository;
        _observacionRepository = observacionRepository;
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
                activity = await _bonitaService.GetActivityByCaseIdAndDisplayName(caseId.ToString(), "Proponer compromiso con una etapa");
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
    /// Obtiene todas las Propuestas de Colaboración propuestas por una Organización en específico, con sus observaciones.
    /// </summary>
    /// <param name="organizacionId">El long de la Organización del cual se quieren ver las propuestas.</param>
    /// <returns>Una lista de Propuestas de Colaboración con sus Etapas asociadas y sus Observaciones.</returns>
    [HttpGet("propone/{organizacionId}")]
    public async Task<IActionResult> GetPropuestasGeneradasOrganizacion(long organizacionId)
    {
        try
        {
            // Obtener las propuestas de la organización
            var propuestas = await _propuestaRepository.FilterAsync(p => p.OrganizacionProponenteId == organizacionId, includes: "Etapa");

            var listado = new List<PropuestaConObservacionesDTO>();

            // Procesar cada propuesta de manera secuencial
            foreach (var p in propuestas)
            {
                // Obtener observaciones asociadas a la propuesta
                var observaciones = await _observacionRepository.FilterAsync(
                    obs => obs.ColaboracionId == p.Etapa.ColaboracionId,
                    orderBy: order => order.OrderByDescending(obs => obs.FechaCarga)
                );

                // Obtener el nombre del proyecto relacionado con la propuesta
                var proyecto = await _proyectoRepository.GetAsync(p.Etapa.ProyectoId);

                // Crear el DTO para la propuesta con observaciones y proyecto
                var propuestaDto = new PropuestaConObservacionesDTO()
                {
                    Id = p.Id,
                    Descripcion = p.Descripcion,
                    CategoriaColaboracion = p.CategoriaColaboracion,
                    EtapaId = p.EtapaId,
                    Etapa = p.Etapa,
                    OrganizacionProponenteId = p.OrganizacionProponenteId,
                    Observaciones = observaciones,
                    Proyecto = proyecto?.Nombre,
                    Estado = p.Estado
                };

                listado.Add(propuestaDto);
            }

            return Ok(listado);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>
    /// Obtiene todas las Propuestas de Colaboración propuestas por una Organización en específico, con sus observaciones.
    /// </summary>
    /// <param name="organizacionId">El long de la Organización del cual se quieren ver las propuestas.</param>
    /// <returns>Una lista de Propuestas de Colaboración con sus Etapas asociadas y sus Observaciones.</returns>
    [HttpGet("recibe/{organizacionId}")]
    public async Task<IActionResult> GetPropuestasRecibidasOrganizacion(long organizacionId)
    {
        try
        {
            // Obtener etapas no completadas de la organización
            var proyectos = await _proyectoRepository.FilterAsync(p => !p.Completado && p.OrganizacionId == organizacionId, 
                                                                orderBy: order => order.OrderByDescending(p => p.Fecha), 
                                                                includes: "Etapas");

            var etapas = proyectos
                            .SelectMany(p => p.Etapas.Where(e => !e.Completada))
                            .Select(e => e.Id)
                            .ToList(); // Convertir a lista para evitar múltiples enumeraciones

            // Obtener propuestas correspondientes a las etapas
            var propuestas = await _propuestaRepository.FilterAsync(p => etapas.Contains(p.EtapaId), includes: "Etapa");

            var listado = new List<PropuestaConObservacionesDTO>();

            // Procesar las propuestas secuencialmente
            foreach (var p in propuestas)
            {
                // Obtener observaciones de cada propuesta
                var observaciones = await _observacionRepository.FilterAsync(
                    obs => obs.ColaboracionId == p.Etapa.ColaboracionId, 
                    orderBy: order => order.OrderByDescending(obs => obs.FechaCarga)
                );

                // Obtener el nombre del proyecto asociado a la propuesta
                var proyecto = await _proyectoRepository.GetAsync(p.Etapa.ProyectoId);

                // Crear el DTO de la propuesta con observaciones y proyecto
                var propuestaDto = new PropuestaConObservacionesDTO()
                {
                    Id = p.Id,
                    Descripcion = p.Descripcion,
                    CategoriaColaboracion = p.CategoriaColaboracion,
                    EtapaId = p.EtapaId,
                    Etapa = p.Etapa,
                    OrganizacionProponenteId = p.OrganizacionProponenteId,
                    Observaciones = observaciones,
                    Proyecto = proyecto?.Nombre
                };

                listado.Add(propuestaDto);
            }

            return Ok(listado);
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
                await _bonitaService.SetVariableByCase(caseId.ToString(), "colaboracionOut", "null", "java.lang.String");

                
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
            try 
            {
                var colaboracionOutJson = await WaitForBonitaVariableUpdate(caseId, "colaboracionOut");
                
                if (colaboracionOutJson == null)
                    return StatusCode(504, "Tiempo de espera agotado: El servicio Cloud tardó demasiado en responder.");

                Etapa etapa = propuesta.Etapa;
                etapa.ColaboracionId = colaboracionOutJson.Id;
                await _etapaRepository.UpdateAsync(etapa, etapa);

                //rechazo las otras
                IEnumerable<PropuestaColaboracion> rechazadas = await _propuestaRepository.FilterAsync(p => p.EtapaId == etapa.Id && p.Id != propuesta.Id && p.Estado == EstadoPropuestaColaboracion.Pendiente);
                foreach (PropuestaColaboracion prop in rechazadas)
                {
                    prop.Estado = EstadoPropuestaColaboracion.Rechazada;
                    await _propuestaRepository.UpdateAsync(prop,prop);
                }

                return Ok(colaboracionOutJson);
            }
            catch(Exception ex)
            {
                 return StatusCode(500, $"Error esperando respuesta del Cloud: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>
    /// Realiza Polling a Bonita hasta que la variable especificada tenga un valor distinto de null/vacío.
    /// </summary>
    private async Task<ColaboracionDTO?> WaitForBonitaVariableUpdate(long caseId, string variableName)
    {
        int intentos = 0;
        int maxIntentos = 10; // 20 intentos * 500ms = 10 segundos de espera máxima
        int delayMs = 500;

        while (intentos < maxIntentos)
        {
            try 
            {
                // Obtenemos el valor crudo (string)
                string jsonResult = await _bonitaService.GetVariableByCaseIdAndName(caseId, variableName);
                Console.WriteLine($"Colaboracion recibida del Cloud: {jsonResult}");

                // Verificamos si tiene un valor válido (Bonita puede devolver "null" como string o string vacío)
                if (!string.IsNullOrEmpty(jsonResult) && jsonResult != "null" && jsonResult != "{}")
                {
                    // Intentamos deserializar
                    var result = JsonSerializer.Deserialize<ColaboracionDTO>(jsonResult, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    Console.WriteLine($"Deserialización {result}");
                    // Si deserializó correctamente, retornamos
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al obtener o deserializar la variable de Bonita: " + ex.Message);
            }

            // Esperamos antes del siguiente intento
            await Task.Delay(delayMs);
            intentos++;
        }

        return null; // Timeout
    }
}