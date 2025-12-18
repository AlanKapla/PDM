using WebApi.Extensions;

internal class Program
{
    private static void Main(string[] args)
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

        builder.Services.AddInfrastructure(builder.Configuration);

        var app = builder.Build();

        app.UseRouting();

        app.UseHttpsRedirection();

        app.UseCookiePolicy();

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
