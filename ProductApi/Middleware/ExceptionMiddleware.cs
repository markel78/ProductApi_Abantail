using System.Net;
using System.Text.Json;
using ProductApi.Domain.Exceptions;

namespace ProductApi.Middleware;

// Patrón Middleware: captura todas las excepciones no controladas
// y devuelve siempre JSON consistente
public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
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
            _logger.LogError(ex, "Excepción no controlada: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, errorCode) = ex switch
        {
            NotFoundException => (HttpStatusCode.NotFound, "NOT_FOUND"),
            ConcurrencyException => (HttpStatusCode.Conflict, "CONCURRENCY_CONFLICT"),
            BusinessRuleException => (HttpStatusCode.BadRequest, "BUSINESS_RULE_VIOLATION"),
            _ => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var body = JsonSerializer.Serialize(new
        {
            errorCode = errorCode,
            message = ex.Message,
            timestamp = DateTime.UtcNow
        });

        await context.Response.WriteAsync(body);
    }
}