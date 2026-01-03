using Business.Interfaces.Constants;
using Microsoft.AspNetCore.Authorization;

namespace WebApi.Authorization;

/// <summary>
/// Authorization requirement that specifies a permission code and its scope
/// </summary>
public sealed record PermissionRequirement(string PermissionCode, PermissionScope Scope) 
    : IAuthorizationRequirement;
