using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using ProductApi.Filters;

namespace ProductApi.Tests
{
    public class ValidationFilterTests
    {
        private static ActionExecutingContext MakeContext(ModelStateDictionary modelState)
        {
            var httpContext = new DefaultHttpContext();
            var routeData = new RouteData();
            var actionDescriptor = new ActionDescriptor();
            var actionContext = new ActionContext(httpContext, routeData, actionDescriptor, modelState);

            return new ActionExecutingContext(
                actionContext,
                filters: [],
                actionArguments: new Dictionary<string, object?>(),
                controller: new object()
            );
        }

        private static ValidationFilter MakeSut() => new();

        [Fact]
        public void OnActionExecuting_ValidModelState_DoesNotSetResult()
        {
            var context = MakeContext(new ModelStateDictionary());

            MakeSut().OnActionExecuting(context);

            Assert.Null(context.Result);
        }

        [Fact]
        public void OnActionExecuting_InvalidModelState_SetsBadRequestResult()
        {
            var modelState = new ModelStateDictionary();
            modelState.AddModelError("Name", "El nombre no puede estar vacío.");
            var context = MakeContext(modelState);

            MakeSut().OnActionExecuting(context);

            Assert.IsType<BadRequestObjectResult>(context.Result);
        }

        [Fact]
        public void OnActionExecuting_InvalidModelState_Returns400StatusCode()
        {
            var modelState = new ModelStateDictionary();
            modelState.AddModelError("Price", "El precio debe ser mayor que 0");
            var context = MakeContext(modelState);

            MakeSut().OnActionExecuting(context);

            var result = Assert.IsType<BadRequestObjectResult>(context.Result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public void OnActionExecuting_InvalidModelState_ReturnsValidationErrorCode()
        {
            var modelState = new ModelStateDictionary();
            modelState.AddModelError("Name", "El nombre no puede estar vacío.");
            var context = MakeContext(modelState);

            MakeSut().OnActionExecuting(context);

            var result = Assert.IsType<BadRequestObjectResult>(context.Result);
            var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
            Assert.Contains("VALIDATION_ERROR", json);
        }

        [Fact]
        public void OnActionExecuting_InvalidModelState_ReturnsErrorsPerField()
        {
            var modelState = new ModelStateDictionary();
            modelState.AddModelError("Name", "El nombre no puede estar vacío.");
            modelState.AddModelError("Price", "El precio debe ser mayor que 0");
            var context = MakeContext(modelState);

            MakeSut().OnActionExecuting(context);

            var result = Assert.IsType<BadRequestObjectResult>(context.Result);
            var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
            Assert.Contains("Name", json);
            Assert.Contains("Price", json);
        }

        [Fact]
        public void OnActionExecuting_InvalidModelState_ReturnsErrorMessages()
        {
            var modelState = new ModelStateDictionary();
            modelState.AddModelError("Name", "El nombre no puede estar vacío.");
            var context = MakeContext(modelState);

            MakeSut().OnActionExecuting(context);

            var result = Assert.IsType<BadRequestObjectResult>(context.Result);
            var options = new System.Text.Json.JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var json = System.Text.Json.JsonSerializer.Serialize(result.Value, options);
            Assert.Contains("El nombre no puede estar vacío.", json);
        }

        [Fact]
        public void OnActionExecuting_MultipleErrorsSameField_ReturnsAllMessages()
        {
            var modelState = new ModelStateDictionary();
            modelState.AddModelError("Name", "El nombre no puede estar vacío.");
            modelState.AddModelError("Name", "El nombre no puede superar 100 caracteres.");
            var context = MakeContext(modelState);

            MakeSut().OnActionExecuting(context);

            var result = Assert.IsType<BadRequestObjectResult>(context.Result);
            var options = new System.Text.Json.JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var json = System.Text.Json.JsonSerializer.Serialize(result.Value, options);
            Assert.Contains("El nombre no puede estar vacío.", json);
            Assert.Contains("El nombre no puede superar 100 caracteres.", json);
        }

        [Fact]
        public void OnActionExecuted_DoesNotThrow()
        {
            var httpContext = new DefaultHttpContext();
            var routeData = new RouteData();
            var actionDescriptor = new ActionDescriptor();
            var modelState = new ModelStateDictionary();
            var actionContext = new ActionContext(httpContext, routeData, actionDescriptor, modelState);
            var executedContext = new ActionExecutedContext(actionContext, filters: [], controller: new object());

            var exception = Record.Exception(() => MakeSut().OnActionExecuted(executedContext));
            Assert.Null(exception);
        }
    }
}
