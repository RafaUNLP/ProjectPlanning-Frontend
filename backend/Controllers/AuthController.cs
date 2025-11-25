using Microsoft.AspNetCore.Mvc;
using backend.DTOs;
using backend.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO loginDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                string bonitaUrl = _configuration["Bonita:BaseUrl"];
                if (string.IsNullOrEmpty(bonitaUrl))
                {
                    return StatusCode(500, "La URL de Bonita no está configurada en el servidor.");
                }
                var bonitaAccess = new Access(bonitaUrl);
                var bonitaSession = await bonitaAccess.LoginAsync(loginDTO.Username, loginDTO.Password);

                if (bonitaSession == null)
                {
                    return Unauthorized(new { message = "Usuario o contraseña de Bonita incorrectos." });
                }

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.BaseAddress = new Uri(bonitaUrl);
                var requestHelper = new RequestHelper(httpClient, bonitaSession.BonitaToken, bonitaSession.JSessionId);
                var tempBonitaService = new BonitaService(requestHelper);

                var userId = await tempBonitaService.GetUserIdByUserName(loginDTO.Username);
                var membresias = await tempBonitaService.GetMembershipsByUserIdAsync(userId);
                string nombreRol = "Sin Rol";

                var primeraMembresia = membresias.FirstOrDefault();

                if (primeraMembresia != null && !string.IsNullOrEmpty(primeraMembresia.role_id))
                {
                    var rolBonita = await tempBonitaService.GetRoleByIdAsync(primeraMembresia.role_id);
                    
                    if (rolBonita != null)
                    {
                        nombreRol = rolBonita.displayName;
                    }
                }


                var appToken = GenerateAppJwt(userId, nombreRol, bonitaSession);

                return Ok(new { token = appToken });
            }
            catch (Exception ex)
            {
                return StatusCode(502, new { message = "Error conectando con el servicio de Bonita.", error = ex.Message });
            }
        }

        private string GenerateAppJwt(string userId, string nombreRol, BonitaSession bonitaSession)
        {
            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("Jwt:Key no está configurada en appsettings.json");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Estos son los "datos" que guardamos dentro del JWT.
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId), 
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("bonita_token", bonitaSession.BonitaToken),
                new Claim("bonita_jsession_id", bonitaSession.JSessionId),
                new Claim("rol", nombreRol)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}