using Business.Implementation.Seeders.Data;
using Entities.Enums;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services;

public sealed class RolePermissionSeederService : IHostedService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<RolePermissionSeederService> logger;

    public RolePermissionSeederService(
        IServiceScopeFactory scopeFactory,
        ILogger<RolePermissionSeederService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var roleRepository = scope.ServiceProvider.GetRequiredService<IRepository<Role>>();
            var permissionRepository = scope.ServiceProvider.GetRequiredService<IRepository<Permission>>();
            var rolePermissionRepository = scope.ServiceProvider.GetRequiredService<IRepository<RolePermission>>();

            logger.LogInformation("Starting Role and Permission seeding...");

            await SeedPermissionsAsync(permissionRepository, cancellationToken);
            await SeedRolesAsync(roleRepository, cancellationToken);
            await SeedRolePermissionsAsync(roleRepository, permissionRepository, rolePermissionRepository, cancellationToken);

            logger.LogInformation("Role and Permission seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed Roles and Permissions");
            throw;
        }
    }

    private async Task SeedPermissionsAsync(IRepository<Permission> repository, CancellationToken cancellationToken)
    {
        var seedData = RolePermissionSeedData.GetPermissions();
        var existingPermissions = (await repository.GetAll()).ToDictionary(p => p.Code);

        foreach (var seed in seedData)
        {
            if (existingPermissions.TryGetValue(seed.Code, out var existing))
            {
                var needsUpdate = false;

                if (existing.Name != seed.Name)
                {
                    existing.Name = seed.Name;
                    needsUpdate = true;
                }

                if (existing.Description != seed.Description)
                {
                    existing.Description = seed.Description;
                    needsUpdate = true;
                }

                if (existing.Scope != seed.Scope)
                {
                    existing.Scope = seed.Scope;
                    needsUpdate = true;
                }

                if (needsUpdate)
                {
                    existing.UpdatedAt = DateTime.UtcNow;
                    await repository.Update(existing);
                    logger.LogInformation("Updated permission: {Code}", seed.Code);
                }
            }
            else
            {
                var permission = new Permission
                {
                    Code = seed.Code,
                    Scope = seed.Scope,
                    Name = seed.Name,
                    Description = seed.Description,
                    IsBuiltIn = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await repository.Insert(permission);
                logger.LogInformation("Created permission: {Code}", seed.Code);
            }
        }

        await repository.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} permissions", seedData.Length);
    }

    private async Task SeedRolesAsync(IRepository<Role> repository, CancellationToken cancellationToken)
    {
        var seedData = RolePermissionSeedData.GetRoles();
        var existingRoles = (await repository.GetAll()).ToDictionary(r => (r.Scope, r.Code));

        foreach (var seed in seedData)
        {
            var key = (seed.Scope, seed.Code);

            if (existingRoles.TryGetValue(key, out var existing))
            {
                var needsUpdate = false;

                if (existing.Name != seed.Name)
                {
                    existing.Name = seed.Name;
                    needsUpdate = true;
                }

                if (existing.Description != seed.Description)
                {
                    existing.Description = seed.Description;
                    needsUpdate = true;
                }

                if (needsUpdate)
                {
                    existing.UpdatedAt = DateTime.UtcNow;
                    await repository.Update(existing);
                    logger.LogInformation("Updated role: {Code}", seed.Code);
                }
            }
            else
            {
                var role = new Role
                {
                    Scope = seed.Scope,
                    Code = seed.Code,
                    Name = seed.Name,
                    Description = seed.Description,
                    IsBuiltIn = seed.IsBuiltIn,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await repository.Insert(role);
                logger.LogInformation("Created role: {Code}", seed.Code);
            }
        }

        await repository.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} roles", seedData.Length);
    }

    private async Task SeedRolePermissionsAsync(
        IRepository<Role> roleRepository,
        IRepository<Permission> permissionRepository,
        IRepository<RolePermission> rolePermissionRepository,
        CancellationToken cancellationToken)
    {
        var seedData = RolePermissionSeedData.GetRolePermissions();
        
        var roles = (await roleRepository.GetAll()).ToDictionary(r => r.Code, r => r.Id);
        var permissions = (await permissionRepository.GetAll()).ToDictionary(p => p.Code, p => p.Id);
        
        var existingMappings = (await rolePermissionRepository.GetAll())
            .Select(rp => (rp.RoleId, rp.PermissionId))
            .ToHashSet();

        var addedCount = 0;

        foreach (var seed in seedData)
        {
            if (!roles.TryGetValue(seed.RoleCode, out var roleId))
            {
                logger.LogWarning("Role not found for mapping: {RoleCode}", seed.RoleCode);
                continue;
            }

            if (!permissions.TryGetValue(seed.PermissionCode, out var permissionId))
            {
                logger.LogWarning("Permission not found for mapping: {PermissionCode}", seed.PermissionCode);
                continue;
            }

            if (!existingMappings.Contains((roleId, permissionId)))
            {
                var rolePermission = new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissionId,
                    CreatedAt = DateTime.UtcNow
                };

                await rolePermissionRepository.Insert(rolePermission);
                addedCount++;
            }
        }

        if (addedCount > 0)
        {
            await rolePermissionRepository.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Added {Count} new role-permission mappings", addedCount);
        }
        else
        {
            logger.LogInformation("No new role-permission mappings to add");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("RolePermissionSeederService stopping.");
        return Task.CompletedTask;
    }
}
