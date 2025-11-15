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
                activity = await _bonitaService.GetActivityByCaseIdAndName(observacionDTO.CaseId.ToString(),"Realizar observacion");
                await _bonitaService.SetVariableByCase(activity.id, "observacion", System.Text.Json.JsonSerializer.Serialize(observacionDTO), "java.lang.String");

                var userName = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userName))
                {
                    return Unauthorized("No se pudo identificar al usuario a partir del token JWT.");
                }

                var userId = await _bonitaService.GetUserIdByUserName(userName);
                await _bonitaService.AssignActivityToUser(activity.id, userId);//necesario?

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
                    Descripcion = observacionDTO.Descripcion
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
    public async Task<IActionResult> ResolverObservacion(ObservacionDTO observacionDTO)
    {
        try
        {
            Observacion? observacion = await _observacionRepository.GetAsync(observacionDTO.Id.Value);

            if (observacion == null)
                return NotFound("Observacion no encontrada");

            BonitaActivityResponse activity;
            try
            {
                activity = await _bonitaService.GetActivityByCaseIdAndName(observacionDTO.CaseId.ToString(),"Resolver observacion");

                var userName = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userName))
                {
                    return Unauthorized("No se pudo identificar al usuario a partir del token JWT.");
                }

                var userId = await _bonitaService.GetUserIdByUserName(userName);
                await _bonitaService.AssignActivityToUser(activity.id, userId);//necesario?

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
                    caseId = observacionDTO.CaseId,
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