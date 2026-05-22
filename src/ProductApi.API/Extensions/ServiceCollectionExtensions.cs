using ProductApi.Application.Interfaces;
using ProductApi.Application.Services;
using ProductApi.Domain.Interfaces;
using ProductApi.Infrastructure.Repositories;

namespace ProductApi.API.Extensions;

/// <summary>
/// Extensiones de registro de servicios para mantener Program.cs limpio.
/// Patrón Facade: oculta la complejidad del registro de dependencias.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Principio D de SOLID: se registran abstracciones, no implementaciones concretas.
        services.AddScoped<IProductRepository, InMemoryProductRepository>();
        services.AddScoped<IProductService, ProductService>();
        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? ["http://localhost:3000", "http://localhost:4200"];

        services.AddCors(options =>
        {
            options.AddPolicy("DefaultCors", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        return services;
    }
}
