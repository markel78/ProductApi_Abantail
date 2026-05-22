using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ProductApi.Filters;

// SOLID - SRP (Single Responsibility Principle):
// Responsabilidad única: interceptar peticiones con modelo inválido
// y devolver 400 antes de que lleguen al controlador.
//
// Patrón Action Filter: separa la validación transversal del código
// de negocio. Se registra una vez en Program.cs y aplica a todos
// los endpoints automáticamente.
public sealed class ValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid) return;

        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
            );

        context.Result = new BadRequestObjectResult(new
        {
            errorCode = "VALIDATION_ERROR",
            message = "Uno o más campos no superaron la validación.",
            errors = errors,
            timestamp = DateTime.UtcNow
        });
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}