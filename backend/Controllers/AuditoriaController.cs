using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.DTOs;
using backend.Repositories;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

namespace backend.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class AuditoriaController : ControllerBase
{
    private readonly ProyectoRepository _proyectoRepository;
    // private readonly ColaboracionRepository _colaboracionRepository;
    private readonly AuditoriaRepository _auditoriaRepository;
    private readonly EtapaRepository _etapaRepository;
    private readonly ObservacionRepository _observacionRepository;
    private readonly BonitaService _bonitaService;
    public AuditoriaController(ProyectoRepository proyectoRepository, ObservacionRepository observacionRepository, //ColaboracionRepository colaboracionoRepository,
                                AuditoriaRepository auditoriaRepository, EtapaRepository etapaRepository, BonitaService bonitaService)
    {
        _proyectoRepository = proyectoRepository;
        _observacionRepository = observacionRepository;
        // _colaboracionRepository = colaboracionoRepository;
        _auditoriaRepository = auditoriaRepository;
        _etapaRepository = etapaRepository;
        _bonitaService = bonitaService;
    }

    [HttpGet]
    public async Task<IActionResult> RecuperarProyectosEnEjecucion()
    {
            try
            {
            // Obtener el valor del header "bonitaJWT"
            string? bonitaJwt = User.FindFirst("bonita_token")?.Value;
            
            if (string.IsNullOrEmpty(bonitaJwt))
            {
                return BadRequest("El header 'bonitaJWT' no está presente.");
            }
            string? username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (username.IsNullOrEmpty())
                return NotFound("No se pudo identificar al usuario a partir del token JWT.");

            int monthlyAuditories = await _auditoriaRepository.CountMonthlyAuditoriesByUsername(username);

            if (monthlyAuditories > 1)
                return BadRequest("El usuario indicado ya realizó dos auditorías este mes");

            
            //si tiene 2 auditorías previas, lo echo
           //----
            //voy a la tarea en Bonita de Login
            //la termino
            //voy a la que lista colabs
            //actualizo localmente
            //le indico si quiero terminar con la variable

            BonitaActivityResponse activity;
            try
            {   
                var idProc = await _bonitaService.GetProcessIdByDisplayName("Auditoría de un proyecto");
                var caseId = await _bonitaService.StartProcessById(idProc);

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("No se pudo identificar al usuario a partir del token JWT.");
                }

                //Según Gemini, las tareas automáticas corren solas y terminan, por lo que no hay que iniciarlas
                activity = await _bonitaService.GetActivityByCaseIdAndDisplayName(caseId.ToString(), "Desplegar los proyectos");
                await _bonitaService.AssignActivityToUser(activity.id, userId);
                
                //conseguir la variable de proceso 'json' que tiene las colaboraciones
                var jsonString = await _bonitaService.GetVariableByCaseIdAndName(caseId, "json");
                var cloudColabs = JsonSerializer.Deserialize<List<ColaboracionDTO>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ColaboracionDTO>();

                var etapaIds = cloudColabs.Select(c => c.EtapaId).ToList();
                var proyectosLocales = await _proyectoRepository.FilterAsync(
                    p => p.Etapas.Any(e => etapaIds.Contains(e.Id)), 
                    includes: "Etapas"
                );

                var colabIds = cloudColabs.Where(c => c.Id.HasValue).Select(c => c.Id.Value).ToList();
                var observacionesLocales = await _observacionRepository.FilterAsync(
                    o => colabIds.Contains(o.ColaboracionId)
                );

                var proyectosResponse = proyectosLocales.Select(p => new
                {
                    p.Id,
                    p.Nombre,
                    p.Descripcion,
                    p.OrganizacionId,
                    p.Fecha,
                    Etapas = p.Etapas
                        .Where(e => etapaIds.Contains(e.Id)) // Filtramos solo las etapas que tienen colaboración activa
                        .Select(e => {
                            // A. Buscamos la colaboración del cloud que corresponde a esta etapa
                            var colabDto = cloudColabs.FirstOrDefault(c => c.EtapaId == e.Id);

                            // B. Si existe, le inyectamos sus observaciones locales
                            if (colabDto != null && colabDto.Id.HasValue)
                            {
                                colabDto.Observaciones = observacionesLocales
                                    .Where(o => o.ColaboracionId == colabDto.Id.Value)
                                    .Select(o => new ObservacionDTO 
                                    {                                         
                                        Id = o.Id,
                                        Descripcion = o.Descripcion,
                                        ColaboracionId = o.ColaboracionId,
                                        FechaCarga = o.FechaCarga,
                                        FechaRealizacion = o.FechaRealizacion,
                                        
                                    })
                                    .ToList();
                            }

                            // C. Retornamos la estructura combinada
                            return new 
                            {
                                e.Id,
                                e.Nombre,
                                e.Descripcion,
                                e.FechaInicio,
                                e.FechaFin,
                                Colaboracion = colabDto // Aquí va el objeto del cloud enriquecido con observaciones
                            };
                        }).ToList()
                }).ToList();
                
                bool finishedActivity = await _bonitaService.CompleteActivityAsync(activity.id);

                if (finishedActivity)
                    return Ok( new
                        {
                            caseId,
                            proyectos = proyectosResponse
                        });
                else
                    return StatusCode(502,"Falló la terminación de la actividad que recupera la colaboraciones en Bonita");

                // //recupero las etapas y de ahí los proyectos para ir al front
                // var etapas = await _etapaRepository.FilterAsync(e => cloudColabs.Select(c => c.EtapaId).Contains(e.Id));
                // var proyectos = await _proyectoRepository.FilterAsync(p => etapas.Select(e => e.ProyectoId).Contains(p.Id),includes:"Etapas,Etapas.Colaboracion,Etapas.Colaboracion.Observaciones");
                
                // bool finishedActivity = await _bonitaService.CompleteActivityAsync(activity.id);

                // if (finishedActivity)
                //     return Ok( new
                //         {
                //             caseId,
                //             proyectos
                //         });//retorno los proyectos y el caseId para poder finalizar la auditoria
                // else
                //     return StatusCode(502,"Falló la terminación de la actividad que recupera la colaboraciones en Bonita");
            }
            catch (Exception ex)
            {
                return StatusCode(502, $"Error en la integración con Bonita: {ex.Message}");
            }

        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> TerminarAuditoría(string caseId)
    {
        try
        {
            var success = await _bonitaService.SetVariableByCase(caseId.ToString(), "terminar", "true", "java.lang.Boolean");
            
            if (!success)
                return StatusCode(502,"Falló la interacción con Bonita");

            string? username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (username.IsNullOrEmpty())
                return NotFound("No se pudo identificar al usuario a partir del token JWT.");

            bool finishedActivity = await _bonitaService.CompleteActivityAsync(caseId);

            if (!finishedActivity)
                return StatusCode(502,"Falló la interacción con Bonita para terminar la auditoría");

            await _auditoriaRepository.AddAsync(new Auditoria()
            {
                Id = Guid.NewGuid(),
                Fecha = DateTime.Now,
                Username = username
            });
            
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

}