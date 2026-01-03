using Entities.Enums;

namespace Business.Implementation.Seeders.Models;

public record RoleSeed(RoleScope Scope, string Code, string Name, string? Description, bool IsBuiltIn);

public record PermissionSeed(RoleScope Scope, string Code, string Name, string? Description);

public record RolePermissionSeed(string RoleCode, string PermissionCode);
