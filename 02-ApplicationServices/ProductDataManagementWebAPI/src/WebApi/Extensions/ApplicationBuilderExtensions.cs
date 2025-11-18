using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using WebApi.Middleware;

namespace WebApi.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
    {
        app.UseMiddleware<ApiExceptionMiddleware>();
        return app;
    }

    public static IApplicationBuilder UseSwaggerWhenDev(this IApplicationBuilder app)
    {
        var env = app.ApplicationServices.GetRequiredService<IHostEnvironment>();
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        return app;
    }
}
