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
    private readonly ColaboracionRepository _colaboracionRepository;
    private readonly AuditoriaRepository _auditoriaRepository;
    private readonly EtapaRepository _etapaRepository;
    private readonly BonitaService _bonitaService;
    public AuditoriaController(ProyectoRepository proyectoRepository, ColaboracionRepository colaboracionoRepository,
                                AuditoriaRepository auditoriaRepository, EtapaRepository etapaRepository, BonitaService bonitaService)
    {
        _proyectoRepository = proyectoRepository;
        _colaboracionRepository = colaboracionoRepository;
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

                var userId = await _bonitaService.GetUserIdByUserName(username);

                //Según Gemini, las tareas automáticas corren solas y terminan, por lo que no hay que iniciarlas
                activity = await _bonitaService.GetActivityByCaseIdAndDisplayName(caseId.ToString(), "Desplegar los proyectos");
                await _bonitaService.AssignActivityToUser(activity.id, userId);
                
                //conseguir la variable de proceso 'json' que tiene las colaboraciones
                var jsonString = await _bonitaService.GetVariableByCaseIdAndName(caseId, "json");
                var cloudColabs = JsonSerializer.Deserialize<List<Colaboracion>>(jsonString,new JsonSerializerOptions {PropertyNameCaseInsensitive = true}) ?? [];

                //updatear las colabs? depende de qué hagamos cuando se asigne 1 y cuando ese 1 la realice
                Colaboracion? actual;
                foreach (Colaboracion cloudColab in cloudColabs)
                {
                    actual = await _colaboracionRepository.GetAsync(cloudColab.Id);
                    if (actual != null)
                    {
                        actual.OrganizacionComprometidaId = cloudColab.OrganizacionComprometidaId;
                        actual.FechaRealizacion = cloudColab.FechaRealizacion;
                        actual.Observaciones = cloudColab.Observaciones;
                    }
                }

                //recupero las etapas y de ahí los proyectos para ir al front
                var etapas = await _etapaRepository.FilterAsync(e => cloudColabs.Select(c => c.EtapaId).Contains(e.Id));
                var proyectos = await _proyectoRepository.FilterAsync(p => etapas.Select(e => e.ProyectoId).Contains(p.Id),includes:"Etapas,Etapas.Colaboracion,Etapas.Colaboracion.Observaciones");
                
                bool finishedActivity = await _bonitaService.CompleteActivityAsync(activity.id);

                if (finishedActivity)
                    return Ok( new
                        {
                            caseId,
                            proyectos
                        });//retorno los proyectos y el caseId para poder finalizar la auditoria
                else
                    return StatusCode(502,"Falló la terminación de la actividad que recupera la colaboraciones en Bonita");
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
            var success = await _bonitaService.SetVariableByCase(caseId.ToString(), "terminar", "true", "boolean");
            
            if (!success)
                return StatusCode(502,"Falló la interacción con Bonita");

            string? username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (username.IsNullOrEmpty())
                return NotFound("No se pudo identificar al usuario a partir del token JWT.");

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