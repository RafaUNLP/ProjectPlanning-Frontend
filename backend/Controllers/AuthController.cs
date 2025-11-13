using Microsoft.AspNetCore.Mvc;
using backend.DTOs;
using backend.Services;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System;
using Microsoft.Extensions.Configuration;

namespace backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
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
                var bonitaAccess = new Access();
                var bonitaSession = await bonitaAccess.LoginAsync(loginDTO.Username, loginDTO.Password);

                if (bonitaSession == null)
                {
                    return Unauthorized(new { message = "Usuario o contraseña de Bonita incorrectos." });
                }

                var appToken = GenerateAppJwt(loginDTO.Username, bonitaSession);

                return Ok(new { token = appToken });
            }
            catch (Exception ex)
            {
                return StatusCode(502, new { message = "Error conectando con el servicio de Bonita.", error = ex.Message });
            }
        }

        private string GenerateAppJwt(string username, BonitaSession bonitaSession)
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
                new Claim(JwtRegisteredClaimNames.Sub, username), 
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("bonita_token", bonitaSession.BonitaToken),
                new Claim("bonita_jsession_id", bonitaSession.JSessionId),
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