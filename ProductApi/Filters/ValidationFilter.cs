using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ProductApi.Filters;

// Antes de que la petición llegue al controlador compruebo que los datos son correctos
// Si algo falla devuelvo un 400 con los errores, si todo ok dejo pasar
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