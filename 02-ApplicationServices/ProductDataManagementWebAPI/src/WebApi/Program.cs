using WebApi.Extensions;

internal class Program
{
    private static void Main(string[] args)
    {
        try
        {
            var builder = WebApplication.CreateBuilder(args);

            // Konfiguracja Kestrel - zwiększenie limitów dla upload plików
            builder.WebHost.ConfigureKestrel(options =>
            {
                // Maksymalny rozmiar żądania: 50 MB
                options.Limits.MaxRequestBodySize = 52428800; // 50 MB in bytes
                
                // Timeout dla odczytu request body: 5 minut (dla dużych plików)
                options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
                options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
            });

            ValidateConfiguration(builder.Configuration);

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
        catch (Exception ex)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("FATAL ERROR DURING APPLICATION STARTUP");
            Console.WriteLine("========================================");
            Console.WriteLine($"Exception Type: {ex.GetType().Name}");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"Stack Trace:\n{ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                Console.WriteLine("\nInner Exception:");
                Console.WriteLine($"Type: {ex.InnerException.GetType().Name}");
                Console.WriteLine($"Message: {ex.InnerException.Message}");
            }
            
            Console.WriteLine("========================================");
            
            // Re-throw to ensure the process exits with error code
            throw;
        }
    }

    private static void ValidateConfiguration(IConfiguration configuration)
    {
        var errors = new List<string>();

        // Required settings
        var requiredSettings = new Dictionary<string, string>
        {
            ["ConnectionStrings:DefaultConnection"] = "SQL Server connection string",
            ["JwtSettings:Secret"] = "JWT signing secret",
            ["BlobStorage:ContainerUrl"] = "Azure Blob Storage container URL",
            ["BlobStorage:QueueUrl"] = "Azure Storage Queue URL"
        };

        foreach (var (key, description) in requiredSettings)
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add($"Missing required configuration: {key} ({description})");
            }
        }

        if (errors.Any())
        {
            Console.WriteLine("========================================");
            Console.WriteLine("CONFIGURATION VALIDATION FAILED");
            Console.WriteLine("========================================");
            Console.WriteLine("The following required settings are missing or empty:");
            Console.WriteLine();
            
            foreach (var error in errors)
            {
                Console.WriteLine($"  ❌ {error}");
            }
            
            Console.WriteLine();
            Console.WriteLine("Please configure these settings in:");
            Console.WriteLine("  - Azure App Service: Configuration > Application settings");
            Console.WriteLine("  - Local development: appsettings.Development.json or User Secrets");
            Console.WriteLine("========================================");
            
            throw new InvalidOperationException(
                $"Application startup failed due to missing configuration. {errors.Count} required setting(s) missing.");
        }

        Console.WriteLine("✅ Configuration validation passed");
    }
}
