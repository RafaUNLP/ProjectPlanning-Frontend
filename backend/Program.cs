using System.Reflection;
using backend.Data;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using backend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
// builder.Services.AddSwaggerGen(options =>
// {
//     // Configurar Swagger para usar OpenAPI 3.0 y especificar la versión del API
//     options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
//     {
//         Title = "Proyect Planning", // Nombre de la API
//         Version = "v1",
//     });
//     // Genera el archivo XML para poder documentar el Swagger
//     var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
//     options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
// });

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

//Añado los servicios
builder.Services.AddScoped<ProyectoRepository>();
builder.Services.AddScoped<EtapaRepository>();

//Añado los repositorios

var app = builder.Build();

//Añade Swagger en development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
app.MapControllers();

app.Run();


