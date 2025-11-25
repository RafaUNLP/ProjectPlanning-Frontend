using System.Reflection;
using backend.Data;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;


var builder = WebApplication.CreateBuilder(args);


// // Configurar JSON con case-insensitive
// builder.Services.AddControllersWithViews()
//     .AddJsonOptions(options =>
//     {
//         options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
//     }
// );

// --- Configuración de JWT ---
// 1. Añadir configuración a appsettings.json (ver abajo)
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("Jwt:Key no está configurada. Añádela a appsettings.json");
}

// 2. Configurar Autenticación
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// 3. Añadir IHttpContextAccessor y HttpClientFactory
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient("bonitaClient")
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        return new HttpClientHandler
        {
            UseCookies = false,            
        };
    });

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Configuración de Swagger para usar JWT
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Ingrese 'Bearer' [espacio] seguido del token JWT en el campo de autorización",
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin() 
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

//Conexión a la BD
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

//Configurar JSON con case-insensitive
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    }
);

//Añado los repositorios
builder.Services.AddScoped<ProyectoRepository>();
builder.Services.AddScoped<EtapaRepository>();
// builder.Services.AddScoped<ColaboracionRepository>();
// builder.Services.AddScoped<OrganizacionRepository>();
builder.Services.AddScoped<AuditoriaRepository>();
builder.Services.AddScoped<ObservacionRepository>();
builder.Services.AddScoped<PropuestaColaboracionRepository>();

//Añado los servicios
builder.Services.AddScoped<RequestHelper>(sp =>
{
    // Obtener un HttpClient de la factoría con nombre "bonitaClient"
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("bonitaClient");
    httpClient.BaseAddress = new Uri(builder.Configuration["Bonita:BaseUrl"]);

    // Obtener el HttpContext actual
    var httpContext = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;
    if (httpContext == null)
    {
        throw new InvalidOperationException("No se pudo obtener HttpContext.");
    }

    // Buscar el token de Bonita guardado en el JWT del usuario
    var bonitaTokenClaim = httpContext.User.FindFirst("bonita_token");
    var jSessionIdClaim = httpContext.User.FindFirst("bonita_jsession_id");

    if (bonitaTokenClaim == null || string.IsNullOrEmpty(bonitaTokenClaim.Value) ||
        jSessionIdClaim == null || string.IsNullOrEmpty(jSessionIdClaim.Value))
    {
        throw new UnauthorizedAccessException("Token de Bonita no encontrado en el JWT.");    
    }

    return new RequestHelper(httpClient, bonitaTokenClaim.Value, jSessionIdClaim.Value);
});

builder.Services.AddScoped<BonitaService>();


var app = builder.Build();

//Añade Swagger en development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseAuthentication(); // 1. Verifica quién es el usuario
app.UseAuthorization();  // 2. Verifica si el usuario tiene permiso

//app.UseHttpsRedirection();
app.MapControllers();

app.Run();


