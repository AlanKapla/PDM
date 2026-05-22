using Business.Implementation.Seeders.Data;
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
        var seedPermissionCodes = seedData.Select(s => s.Code).ToHashSet();

        // Update or create permissions
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

        // Remove permissions that no longer exist in seed data (only built-in ones)
        var permissionsToRemove = existingPermissions.Values
            .Where(p => p.IsBuiltIn && !seedPermissionCodes.Contains(p.Code))
            .ToList();

        foreach (var permission in permissionsToRemove)
        {
            await repository.Delete(permission);
            logger.LogWarning("Removed obsolete permission: {Code}", permission.Code);
        }

        await repository.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} permissions, removed {RemovedCount} obsolete permissions", 
            seedData.Length, permissionsToRemove.Count);
    }

    private async Task SeedRolesAsync(IRepository<Role> repository, CancellationToken cancellationToken)
    {
        var seedData = RolePermissionSeedData.GetRoles();
        var existingRoles = (await repository.GetAll()).ToDictionary(r => (r.Scope, r.Code));
        var seedRoleKeys = seedData.Select(s => (s.Scope, s.Code)).ToHashSet();

        // Update or create roles
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

        // Remove roles that no longer exist in seed data (only built-in ones)
        var rolesToRemove = existingRoles.Values
            .Where(r => r.IsBuiltIn && !seedRoleKeys.Contains((r.Scope, r.Code)))
            .ToList();

        foreach (var role in rolesToRemove)
        {
            await repository.Delete(role);
            logger.LogWarning("Removed obsolete role: {Code} (Scope: {Scope})", role.Code, role.Scope);
        }

        await repository.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} roles, removed {RemovedCount} obsolete roles", 
            seedData.Length, rolesToRemove.Count);
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
        
        var existingMappings = await rolePermissionRepository.GetAll();
        var existingMappingsSet = existingMappings
            .Select(rp => (rp.RoleId, rp.PermissionId))
            .ToHashSet();

        var addedCount = 0;
        var expectedMappings = new HashSet<(Guid RoleId, Guid PermissionId)>();

        // Add new mappings
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

            expectedMappings.Add((roleId, permissionId));

            if (!existingMappingsSet.Contains((roleId, permissionId)))
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

        // Remove mappings that are no longer in seed data
        var mappingsToRemove = existingMappings
            .Where(rp => !expectedMappings.Contains((rp.RoleId, rp.PermissionId)))
            .ToList();

        foreach (var mapping in mappingsToRemove)
        {
            await rolePermissionRepository.Delete(mapping);
            logger.LogWarning("Removed obsolete role-permission mapping: RoleId={RoleId}, PermissionId={PermissionId}", 
                mapping.RoleId, mapping.PermissionId);
        }

        if (addedCount > 0 || mappingsToRemove.Count > 0)
        {
            await rolePermissionRepository.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Added {AddedCount} new role-permission mappings, removed {RemovedCount} obsolete mappings", 
                addedCount, mappingsToRemove.Count);
        }
        else
        {
            logger.LogInformation("No changes to role-permission mappings");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("RolePermissionSeederService stopping.");
        return Task.CompletedTask;
    }
}
