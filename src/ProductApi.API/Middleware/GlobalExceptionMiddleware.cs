using System.Net;
using System.Text.Json;
using ProductApi.Domain.Exceptions;

namespace ProductApi.API.Middleware;

/// <summary>
/// Middleware global de manejo de errores.
/// Patrón Chain of Responsibility: captura todas las excepciones no tratadas
/// y devuelve respuestas JSON consistentes.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errorCode, message) = exception switch
        {
            ProductNotFoundException e   => (HttpStatusCode.NotFound,   "PRODUCT_NOT_FOUND",    e.Message),
            ProductConcurrencyException e => (HttpStatusCode.Conflict,   "CONCURRENCY_CONFLICT", e.Message),
            DuplicateProductException e  => (HttpStatusCode.Conflict,   "DUPLICATE_PRODUCT",    e.Message),
            ArgumentException e          => (HttpStatusCode.BadRequest,  "INVALID_ARGUMENT",     e.Message),
            _                            => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred.")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = (int)statusCode;

        var body = JsonSerializer.Serialize(new
        {
            errorCode,
            message,
            timestamp = DateTime.UtcNow
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await context.Response.WriteAsync(body);
    }
}
