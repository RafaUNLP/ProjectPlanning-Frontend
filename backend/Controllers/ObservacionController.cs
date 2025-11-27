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
public class ObservacionController : ControllerBase
{
    private readonly ObservacionRepository _observacionRepository;
    private readonly BonitaService _bonitaService;
    public ObservacionController(ObservacionRepository observacionRepository, BonitaService bonitaService)
    {
        _observacionRepository = observacionRepository;
        _bonitaService = bonitaService;
    }

    [HttpPost]
    public async Task<IActionResult> CargarObservacion(ObservacionDTO observacionDTO)
    {
        try
        {
            BonitaActivityResponse activity;
            try
            {
                BonitaActivityResponse desplegarActivity = await _bonitaService.GetActivityByCaseIdAndDisplayName(observacionDTO.CaseId.ToString(),"Desplegar los proyectos");
                await _bonitaService.CompleteActivityAsync(desplegarActivity.id);

                activity = await _bonitaService.GetActivityByCaseIdAndDisplayName(observacionDTO.CaseId.ToString(),"Realizar observación");

                var userName = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userName))
                {
                    return Unauthorized("No se pudo identificar al usuario a partir del token JWT.");
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("No se pudo identificar al usuario a partir del token JWT.");
                }
                await _bonitaService.AssignActivityToUser(activity.id, userId);

                await _bonitaService.SetVariableByCase(observacionDTO.CaseId.ToString(),"observacion",JsonSerializer.Serialize(new
                {
                    colaboracionId = observacionDTO.ColaboracionId,
                    descripcion = observacionDTO.Descripcion
                }),"java.lang.String");

            }
            catch (Exception ex)
            {
                return StatusCode(502, $"Error en la integración con Bonita: {ex.Message}");
            }

            bool finishedActivity = await _bonitaService.CompleteActivityAsync(activity.id);//completa la actividad

            if (finishedActivity)
            {
                Observacion created = await _observacionRepository.AddAsync(new Observacion()
                {
                    Id = new Guid(),
                    ColaboracionId = observacionDTO.ColaboracionId,
                    Descripcion = observacionDTO.Descripcion,
                    CaseId = observacionDTO.CaseId.Value
                });

                return Ok(new
                {
                    caseId = observacionDTO.CaseId,
                    observacion = created
                });
            }
            else
            {
                return StatusCode(502,"Falló la terminación de la actividad en Bonita");   
            }

        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPut]
    public async Task<IActionResult> ResolverObservacion(ResolverObservacionDTO observacionDTO)
    {
        try
        {
            Observacion? observacion = await _observacionRepository.GetAsync(observacionDTO.Id);

            if (observacion == null)
                return NotFound("Observacion no encontrada");

            BonitaActivityResponse activity;
            try
            {
                activity = await _bonitaService.GetActivityByCaseIdAndDisplayName(observacion.CaseId.ToString(),"Resolver observación");

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

            bool finishedActivity = await _bonitaService.CompleteActivityAsync(activity.id);//completa la actividad

            if (finishedActivity)
            {
                observacion.FechaRealizacion = DateTime.Now;
                observacion = await _observacionRepository.UpdateAsync(observacion,observacion);
                return Ok(new
                {
                    caseId = observacion.CaseId,
                    observacion = observacion
                });
            }
            else
            {
                return StatusCode(502,"Falló la terminación de la actividad en Bonita");   
            }

        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }


}