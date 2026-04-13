using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Reflection;

namespace Entities.Context
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // Try multiple possible base paths for appsettings.json
            var possiblePaths = new[]
            {
                Directory.GetCurrentDirectory(),
                Path.GetDirectoryName(typeof(AppDbContextFactory).Assembly.Location) ?? "",
                Path.Combine(Directory.GetCurrentDirectory(), "src", "Entities"),
                AppContext.BaseDirectory
            };

            IConfiguration? configuration = null;
            string? workingPath = null;

            foreach (var basePath in possiblePaths)
            {
                var appSettingsPath = Path.Combine(basePath, "appsettings.json");
                if (File.Exists(appSettingsPath))
                {
                    string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

                    configuration = new ConfigurationBuilder()
                        .SetBasePath(basePath)
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                        .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
                        .AddEnvironmentVariables()
                        .Build();
                    workingPath = basePath;
                    break;
                }
            }

            if (configuration == null)
            {
                throw new InvalidOperationException(
                    $"Could not find appsettings.json in any of the following paths:\n" +
                    string.Join("\n", possiblePaths.Select(p => Path.Combine(p, "appsettings.json"))));
            }

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    $"ConnectionString 'DefaultConnection' not found in appsettings.json at: {workingPath}");
            }

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
