using WebApi.Extensions;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddInfrastructure(builder.Configuration);

        var app = builder.Build();

        app.UseRouting();
        
        app.UseCors("AllowFrontend");

        app.UseWebSockets();

        app.UseGlobalExceptionHandling();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.MapHub<WebApi.Hubs.NotificationHub>("api/hubs/notifications")
            .RequireCors("AllowFrontend");

        app.MapHealthChecks("api/health");

        app.Run();
    }
}