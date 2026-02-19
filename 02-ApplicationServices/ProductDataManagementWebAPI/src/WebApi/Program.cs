using Microsoft.IdentityModel.Logging;
using WebApi.Extensions;
using Business.AIAgent.Extensions;

internal class Program
{
    private static void Main(string[] args)
    {
        // Enable PII logging FIRST - before any authentication setup
        // ⚠️ WARNING: Only for development! Shows sensitive token data in logs
        IdentityModelEventSource.ShowPII = true;

        var builder = WebApplication.CreateBuilder(args);

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

        // ✅ Register AI Plugins with Semantic Kernel
        app.Services.UseAIPlugins();

        app.UseWebSockets();

        app.UseRouting();

        if(!builder.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseCors("AllowFrontend");

        app.UseGlobalExceptionHandling();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.MapHub<WebApi.Hubs.NotificationHub>("/api/hubs/notifications")
            .RequireCors("AllowFrontend");

        app.MapHub<WebApi.Hubs.MessageHub>("/api/hubs/messages")
            .RequireCors("AllowFrontend");

        app.MapHealthChecks("/api/health");

        app.Run();
    }
}
