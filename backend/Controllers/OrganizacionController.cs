using Microsoft.AspNetCore.Mvc;
using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;

namespace backend.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class OrganizacionController : ControllerBase
{
    private readonly BonitaService _bonitaService;

    public OrganizacionController(BonitaService bonitaService)
    {
        _bonitaService = bonitaService;
    }

    /// <summary>
    /// Recupera un usuario de Bonita con su Rol principal.
    /// </summary>
    /// <param name="id">ID del usuario en Bonita</param>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUsuarioBonita(string id)
    {
        try
        {
            var bonitaUser = await _bonitaService.GetUserNameByUserId(id);

            if (bonitaUser == null)
            {
                return NotFound($"No se encontró el usuario con id {id} en Bonita.");
            }

            var membresias = await _bonitaService.GetMembershipsByUserIdAsync(id);
            
            string nombreRol = "Sin Rol";

            var primeraMembresia = membresias.FirstOrDefault();

            if (primeraMembresia != null && !string.IsNullOrEmpty(primeraMembresia.role_id))
            {
                var rolBonita = await _bonitaService.GetRoleByIdAsync(primeraMembresia.role_id);
                
                if (rolBonita != null)
                {
                    nombreRol = rolBonita.displayName;
                }
            }

            long.TryParse(bonitaUser.id, out long idNumerico);

            var usuarioDto = new OrganizacionDTO
            {
                Id = idNumerico,
                UserName = bonitaUser.userName,
                Nombre = bonitaUser.firstName,
                Apellido = bonitaUser.lastName,
                Enabled = bonitaUser.enabled == "true",
                Rol = nombreRol
            };

            return Ok(usuarioDto);
        }
        catch (Exception ex)
        {
            return StatusCode(502, $"Error obteniendo usuario de Bonita: {ex.Message}");
        }
    }
}