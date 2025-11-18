using WebApi.Extensions;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddApiBasics()
            .AddDatabase(builder.Configuration)
            .AddCqrs()
            .AddJwt(builder.Configuration)
            .AddAppRepositories()
            .AddAppServices();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins("http://localhost:5173")
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        

        var app = builder.Build();

        app.UseCors("AllowFrontend");

        app.UseGlobalExceptionHandling();

        app.UseSwaggerWhenDev();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.MapHealthChecks("/health");

        app.Run();
    }
}