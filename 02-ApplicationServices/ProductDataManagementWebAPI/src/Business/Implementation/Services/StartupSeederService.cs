using Business.Interfaces.Configurations;
using Entities.Enums;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services;

public sealed class StartupSeederService : IHostedService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly SeedSettings seedSettings;
    private readonly ILogger<StartupSeederService> logger;

    public StartupSeederService(
        IServiceScopeFactory scopeFactory,
        IOptions<SeedSettings> seedSettings,
        ILogger<StartupSeederService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.seedSettings = seedSettings.Value;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(seedSettings.SuperAdminEmail))
        {
            logger.LogInformation("SuperAdminEmail not configured in Seed settings. Skipping seed.");
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IRepository<User>>();

            var superAdminEmail = seedSettings.SuperAdminEmail.Trim().ToLowerInvariant();

            var existingUser = await userRepository.GetFirstBySearch(
                u => u.Email.ToLower() == superAdminEmail);

            if (existingUser != null)
            {
                logger.LogInformation("SuperAdmin user with email {Email} already exists. Skipping seed.", superAdminEmail);
                return;
            }

            var superAdminUser = new User
            {
                Email = superAdminEmail,
                FirstName = "Super",
                LastName = "Admin",
                AzureAdB2CObjectId = string.Empty,
                IsActive = true,
                SystemRole = SystemRole.SuperAdmin,
                CreatedAt = DateTime.UtcNow
            };

            await userRepository.Insert(superAdminUser);
            await userRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation("SuperAdmin user created successfully with email {Email} and Id {UserId}", 
                superAdminEmail, superAdminUser.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed SuperAdmin user");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("StartupSeederService stopping.");
        return Task.CompletedTask;
    }
}
