using Microsoft.OpenApi.Models;
using ProductApi.API.Extensions;
using ProductApi.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers ──────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── Swagger / OpenAPI ─────────────────────────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Product API",
        Version     = "v1",
        Description = "API REST para gestión de productos con arquitectura en capas.",
        Contact     = new OpenApiContact { Name = "Dev Team", Email = "dev@example.com" }
    });

    // Incluye comentarios XML de los controladores
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

// ── Application services ──────────────────────────────────────────────────────
builder.Services.AddApplicationServices();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCorsPolicy(builder.Configuration);

// ── Health Checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ── HTTPS ─────────────────────────────────────────────────────────────────────
builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = 7001;
});

// ── Logging ───────────────────────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseMiddleware<GlobalExceptionMiddleware>(); // 1. Captura excepciones globales

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product API v1");
        c.RoutePrefix = string.Empty; // Swagger en la raíz
    });
}

app.UseHttpsRedirection();
app.UseCors("DefaultCors");
app.UseAuthorization();

// ── Health check endpoint ─────────────────────────────────────────────────────
app.MapHealthChecks("/health");

app.MapControllers();

app.Run();

// Necesario para los tests de integración
public partial class Program { }
