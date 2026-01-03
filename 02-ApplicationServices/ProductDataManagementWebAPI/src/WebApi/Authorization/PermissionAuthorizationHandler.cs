using Business.Implementation.Services;
using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace WebApi.Authorization;

/// <summary>
/// Authorization handler that validates permissions based on their scope (Global, Tenant, Project, Resource)
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ICurrentUser currentUser;
    private readonly AccessService accessService;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ILogger<PermissionAuthorizationHandler> logger;

    public PermissionAuthorizationHandler(
        ICurrentUser currentUser,
        AccessService accessService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PermissionAuthorizationHandler> logger)
    {
        this.currentUser = currentUser;
        this.accessService = accessService;
        this.httpContextAccessor = httpContextAccessor;
        this.logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            logger.LogWarning("HttpContext is null in PermissionAuthorizationHandler");
            context.Fail();
            return;
        }

        var routeData = httpContext.GetRouteData();
        
        Guid tenantId = Guid.Empty;
        Guid? projectId = null;
        Guid? resourceId = null;

        // Extract required identifiers based on permission scope
        switch (requirement.Scope)
        {
            case PermissionScope.Global:
                // Global permissions don't require any route parameters
                logger.LogDebug("Processing global permission {Permission}", requirement.PermissionCode);
                break;
                
            case PermissionScope.Tenant:
                // Tenant permissions require tenantId
                if (!TryGetGuid(routeData, "tenantId", out var tId))
                {
                    logger.LogWarning(
                        "TenantId not found or invalid in route for tenant-scoped permission {Permission}",
                        requirement.PermissionCode);
                    context.Fail();
                    return;
                }
                tenantId = tId;
                break;
                
            case PermissionScope.Project:
                // Project permissions require BOTH tenantId and projectId
                if (!TryGetGuid(routeData, "tenantId", out var tIdForProject))
                {
                    logger.LogWarning(
                        "TenantId not found or invalid in route for project-scoped permission {Permission}",
                        requirement.PermissionCode);
                    context.Fail();
                    return;
                }
                tenantId = tIdForProject;
                
                if (!TryGetGuid(routeData, "projectId", out var pId))
                {
                    logger.LogWarning(
                        "ProjectId not found or invalid in route for project-scoped permission {Permission}",
                        requirement.PermissionCode);
                    context.Fail();
                    return;
                }
                projectId = pId;
                break;
                
            case PermissionScope.Resource:
                // Resource permissions require tenantId, projectId AND resourceId
                if (!TryGetGuid(routeData, "tenantId", out var tIdForResource))
                {
                    logger.LogWarning(
                        "TenantId not found or invalid in route for resource-scoped permission {Permission}",
                        requirement.PermissionCode);
                    context.Fail();
                    return;
                }
                tenantId = tIdForResource;
                
                if (!TryGetGuid(routeData, "projectId", out var pIdForResource))
                {
                    logger.LogWarning(
                        "ProjectId not found or invalid in route for resource-scoped permission {Permission}",
                        requirement.PermissionCode);
                    context.Fail();
                    return;
                }
                projectId = pIdForResource;
                
                resourceId = ExtractResourceId(routeData);
                if (resourceId == null)
                {
                    logger.LogWarning(
                        "ResourceId not found in route for resource-scoped permission {Permission}",
                        requirement.PermissionCode);
                    context.Fail();
                    return;
                }
                break;
        }

        var resource = new ResourceRef(tenantId, projectId, resourceId);

        try
        {
            var authorized = await accessService.AuthorizeAsync(
                currentUser,
                requirement.PermissionCode,
                resource,
                httpContext.RequestAborted);

            if (authorized)
            {
                context.Succeed(requirement);
                logger.LogDebug(
                    "Authorization succeeded for user {UserId} with permission {Permission} (scope: {Scope}) on tenant {TenantId}, project {ProjectId}, resource {ResourceId}",
                    currentUser.Id,
                    requirement.PermissionCode,
                    requirement.Scope,
                    tenantId,
                    projectId,
                    resourceId);
            }
            else
            {
                logger.LogWarning(
                    "Authorization failed for user {UserId} with permission {Permission} (scope: {Scope}) on tenant {TenantId}, project {ProjectId}, resource {ResourceId}",
                    currentUser.Id,
                    requirement.PermissionCode,
                    requirement.Scope,
                    tenantId,
                    projectId,
                    resourceId);
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, 
                "Error during authorization for user {UserId} with permission {Permission} (scope: {Scope})",
                currentUser.Id,
                requirement.PermissionCode,
                requirement.Scope);
            context.Fail();
        }
    }

    /// <summary>
    /// Tries to extract a Guid from route data
    /// </summary>
    private static bool TryGetGuid(RouteData routeData, string key, out Guid value)
    {
        value = default;
        return routeData.Values.TryGetValue(key, out var obj) &&
               Guid.TryParse(obj?.ToString(), out value);
    }

    /// <summary>
    /// Extracts resource ID from various possible route parameters
    /// </summary>
    private static Guid? ExtractResourceId(RouteData routeData)
    {
        // Common resource ID parameter names in routes
        var resourceIdKeys = new[]
        {
            "fileId",           // Pliki
            "estimateId",       // Kosztorysy
            "scheduleId",       // Harmonogramy
            "workScheduleId",   // Harmonogramy (alternatywna nazwa)
            "workId",           // Prace w harmonogramie
            "stageId",          // Etapy harmonogramu
            "costId",           // Koszty projektu
            "packageId",        // Paczki plików
            "versionId",        // Wersje plików
            "groupId",          // Grupy projektowe
            "notificationId",   // Powiadomienia
            "invitationId",     // Zaproszenia
            "chatId",           // Czaty
            "messageId",        // Wiadomości
            "templateId",       // Szablony kosztorysów
            "resourceId",       // Ogólny resource ID
            "id"                // Fallback - ogólny ID
        };

        foreach (var key in resourceIdKeys)
        {
            if (routeData.Values.TryGetValue(key, out var value) && 
                Guid.TryParse(value?.ToString(), out var parsedId))
            {
                return parsedId;
            }
        }

        return null;
    }
}
