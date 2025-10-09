using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.DTOs;
using backend.Repositories;
using backend.Services;
using Microsoft.VisualBasic;

namespace backend.Controllers;

[ApiController]
[Route("[controller]")]
public class OrganizacionController : ControllerBase
{
    private readonly OrganizacionRepository _organizacionRepository;
    private readonly BonitaService _bonitaService;
    public OrganizacionController(OrganizacionRepository organizacionRepository, BonitaService bonitaService)
    {
        _organizacionRepository = organizacionRepository;
        _bonitaService = bonitaService;
    }

    [HttpPost]
    public async Task<IActionResult> CrearOrganizacion(CrearOrganizacionDTO organizacionDTO)
    {
        try
        {
            bool nombreOrganizacionEnUso = await _organizacionRepository.Exist(o => o.Nombre.ToLower() == organizacionDTO.Nombre.ToLower());

            if (nombreOrganizacionEnUso)
                return Conflict("El nombre de la organización ya está en uso, por favor use uno distinto.");

            Organizacion organizacion = await _organizacionRepository.AddAsync(new Organizacion()
            {
                Id = Guid.NewGuid(),
                Nombre = organizacionDTO.Nombre,
                Contraseña = organizacionDTO.Contraseña
            });

            //¿todo esto va?
            var idProc = await _bonitaService.GetProcessIdByName("Prueba1");//recupera id del proceso
            var caseId = await _bonitaService.StartProcessById(idProc);//inicia una instancia del mismo
            var suc = await _bonitaService.SetVariableByCase(caseId.ToString(), "var1", "valor1", "java.lang.String");//le instancia variables de prueba
            var activity = await _bonitaService.GetActivityByCaseId(caseId.ToString());//recupera el id de la actividad
            Console.WriteLine($"Actividad: {activity}");
            var userId = await _bonitaService.GetUserIdByUserName("walter.bates");//hay que asignar un usuario a la actividad para completarla, recupera el id usuario en bonita
            await _bonitaService.AssignActivityToUser(activity.id, userId);//le asigna la actividad al usuario
            bool finishedActivity = await _bonitaService.CompleteActivityAsync(activity.id);//completa la actividad

            if (finishedActivity)
                return Ok(new OrganizacionDTO()
                {
                    Id = organizacion.Id,
                    Nombre = organizacion.Nombre
                });
            else
                return StatusCode(502,"Falló la terminación de la actividad en Bonita");
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> RecuperarOrganizacion(Guid id)
    {
        try
        {
            Organizacion? buscada = await _organizacionRepository.GetAsyncWithIncludes(id, includes: "Proyectos,ColaboracionesParaRealizar");

            if (buscada == null)
                return NotFound();

            return Ok(new OrganizacionDTO()
            {
                Nombre = buscada.Nombre,
                Proyectos = buscada.Proyectos,
                ColaboracionesComprometida = buscada.ColaboracionesComprometida,
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}