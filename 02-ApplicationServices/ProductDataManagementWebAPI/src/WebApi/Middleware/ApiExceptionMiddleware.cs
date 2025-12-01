namespace WebApi.Middleware
{
    using Business.Interfaces.Exceptions;
    using System.Net;
    using System.Text.Json;

    public class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<ApiExceptionMiddleware> _logger = logger;

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
                _logger.LogError(ex, "Unhandled exception");
                await HandleUnknownExceptionAsync(context, ex);
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

        private static async Task HandleUnknownExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new
            {
                error = "InternalServerError",
                message = ex.Message
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
