using Microsoft.AspNetCore.Mvc;
using ProductApi.Application.Interfaces;
using ProductApi.Application.Services;
using ProductApi.Domain.Interfaces;
using ProductApi.Filters;
using ProductApi.Infrastructure.Repositories;
using ProductApi.Middleware;

// ── Para activar EF Core, descomentar estas líneas:
// using Microsoft.EntityFrameworkCore;
// using ProductApi.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ── Controladores ──────────────────────────────────────────
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// ── Swagger / OpenAPI ──────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "ProductApi",
        Version = "v1",
        Description = "API CRUD de productos con arquitectura en capas"
    });
});

// ── CORS ───────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(
                builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? ["http://localhost:3000"])
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ── Health Checks ──────────────────────────────────────────
builder.Services.AddHealthChecks();

// ── Inyección de dependencias ──────────────────────────────
// Repositorio activo: en memoria (sin base de datos)
builder.Services.AddScoped<IProductRepository, InMemoryProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();

// ── Para activar EF Core, sustituir las dos líneas anteriores por:
// builder.Services.AddDbContext<AppDbContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// builder.Services.AddScoped<IProductRepository, EfProductRepository>();

// ── HTTPS ──────────────────────────────────────────────────
builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = 7001;
});

var app = builder.Build();

// ── Pipeline ───────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProductApi v1"));
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();