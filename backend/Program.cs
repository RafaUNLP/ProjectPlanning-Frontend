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



var access = new Access();

// 1. Login
RequestHelper rh = await access.LoginAsync("walter.bates", "bpm");

BonitaService bonitaService = new BonitaService(rh);

var id = await bonitaService.GetProcessIdByName("Prueba1");
var caseId = await bonitaService.StartProcessById(id);
var suc = await bonitaService.SetVariableByCase(caseId.ToString(), "var1", "valor1", "java.lang.String");
Console.WriteLine($"Put exitoso?:{suc} ");
await bonitaService.SetVariableByCase(caseId.ToString(), "var2", "valor2", "java.lang.String");
var activity = await bonitaService.GetActivityByCaseId(caseId.ToString());
Console.WriteLine($"Actividad: {activity}" );
//hay que asignar un usuario a la actividad para completarla
var userId = await bonitaService.GetUserIdByUserName("walter.bates");
await bonitaService.AssignActivityToUser(activity.id, userId);
await bonitaService.CompleteActivityAsync(activity.id);




app.Run();


