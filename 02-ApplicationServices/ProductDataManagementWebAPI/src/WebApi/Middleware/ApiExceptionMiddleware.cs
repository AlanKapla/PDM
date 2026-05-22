namespace WebApi.Middleware
{
    using Business.Interfaces.Exceptions;
    using Microsoft.Extensions.Hosting;
    using System.Net;
    using System.Text.Json;

    public class ApiExceptionMiddleware(
        RequestDelegate next,
        ILogger<ApiExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<ApiExceptionMiddleware> _logger = logger;
        private readonly IHostEnvironment _environment = environment;

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (UnauthorizedApiException)
            { 
                _logger.LogWarning("UnauthorizedUser");
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            }
            catch (ApiException ex)
            {
                _logger.LogWarning(ex, "API Exception: {Message}", ex.Message);
                await HandleApiExceptionAsync(context, ex);
            }
           catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
                await HandleUnknownExceptionAsync(context, ex, _environment.IsDevelopment());
            }
        }

        private static async Task HandleApiExceptionAsync(HttpContext context, ApiException ex)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)ex.GetStatusCode();

            var response = new
            {
                error = ex.Reason.ToString(),
                message = ex.Message,
                objectType = ex.ObjectType,
                objectId = ex.ObjectId
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }

        private static async Task HandleUnknownExceptionAsync(HttpContext context, Exception ex, bool isDevelopment)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            string message = isDevelopment
                ? ex.Message
                : "Wystąpił błąd wewnętrzny serwera.";

            var response = new
            {
                error = "InternalServerError",
                message
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
