using Microsoft.IdentityModel.Logging;
using Business.AIAgent.Registration;
using Chat.Registration;
using WebApi.Extensions;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        if (builder.Environment.IsDevelopment())
        {
            IdentityModelEventSource.ShowPII = true;
        }

        // Set Azure environment variables in Development mode
        if (builder.Environment.IsDevelopment())
        {
            var azureConfig = builder.Configuration.GetSection("Azure");
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", azureConfig["ClientId"]);
            Environment.SetEnvironmentVariable("AZURE_TENANT_ID", azureConfig["TenantId"]);
            Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", azureConfig["ClientSecret"]);
            Environment.SetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULTCONNECTION", connectionString);
        }

        // Konfiguracja Kestrel - zwiększenie limitów dla upload plików
        builder.WebHost.ConfigureKestrel(options =>
        {
            // Maksymalny rozmiar żądania: 50 MB
            options.Limits.MaxRequestBodySize = 52428800; // 50 MB in bytes

            // Timeout dla odczytu request body: 5 minut (dla dużych plików)
            options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
            options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
        });

        builder.Services.AddInfrastructure(builder.Configuration);

        var app = builder.Build();

        app.UseGlobalExceptionHandling();

        app.UseWebSockets();

        app.UseRequestLocalization();

        app.UseRouting();

        if(!builder.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseCors("AllowFrontend");

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.MapHub<WebApi.Hubs.NotificationHub>("/api/hubs/notifications")
            .RequireCors("AllowFrontend");

        app.MapHub<WebApi.Hubs.MessageHub>("/api/hubs/messages")
            .RequireCors("AllowFrontend");

        app.MapHub<WebApi.Hubs.AIHub>("/api/hubs/ai")
            .RequireCors("AllowFrontend");

        app.MapHub<WebApi.Hubs.TechnicalDocumentationHub>("/api/hubs/technical-documentation")
            .RequireCors("AllowFrontend");

        app.MapChatHub()
            .RequireCors("AllowFrontend");

        app.MapHealthChecks("/api/health");

        app.Run();
    }
}
