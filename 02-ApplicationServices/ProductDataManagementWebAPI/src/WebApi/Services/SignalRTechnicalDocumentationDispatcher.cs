using Business.Interfaces.Services;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Entities.Enums;
using Entities.Models.Projects;
using Entities.Models.Users;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;
using WebApi.Hubs;

namespace WebApi.Services;

public sealed class SignalRTechnicalDocumentationDispatcher : ITechnicalDocumentationDispatcher
{
    private readonly IHubContext<TechnicalDocumentationHub, ITechnicalDocumentationClient> hubContext;
    private readonly IRepository<ProjectMemberModulePermission> modulePermissionRepository;
    private readonly IRepository<ProjectMember> projectMemberRepository;
    private readonly IReadRepository<User> userRepository;
    private readonly ILogger<SignalRTechnicalDocumentationDispatcher> logger;

    public SignalRTechnicalDocumentationDispatcher(
        IHubContext<TechnicalDocumentationHub, ITechnicalDocumentationClient> hubContext,
        IRepository<ProjectMemberModulePermission> modulePermissionRepository,
        IRepository<ProjectMember> projectMemberRepository,
        IReadRepository<User> userRepository,
        ILogger<SignalRTechnicalDocumentationDispatcher> logger)
    {
        this.hubContext = hubContext;
        this.modulePermissionRepository = modulePermissionRepository;
        this.projectMemberRepository = projectMemberRepository;
        this.userRepository = userRepository;
        this.logger = logger;
    }

    public async Task DispatchCompletedAsync(
        TechnicalDocumentationProcessingResultDto payload,
        CancellationToken cancellationToken)
    {
        HashSet<Guid> recipientUserIds = await GetRecipientUserIdsAsync(payload, cancellationToken);

        if (recipientUserIds.Count == 0)
        {
            logger.LogWarning(
                "No recipients found for technical documentation notification {DocumentationId}",
                payload.DocumentationId);
            return;
        }

        List<User> users = (await userRepository.GetBySearch(
            u => recipientUserIds.Contains(u.Id))).ToList();

        List<string> azureObjectIds = users
            .Select(u => u.AzureAdB2CObjectId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

        if (azureObjectIds.Count == 0)
        {
            logger.LogWarning(
                "No Azure AD B2C object IDs found for technical documentation notification {DocumentationId}",
                payload.DocumentationId);
            return;
        }

        foreach (string azureObjectId in azureObjectIds)
        {
            try
            {
                await hubContext.Clients.User(azureObjectId)
                    .ProcessingCompleted(payload);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to dispatch technical documentation notification to user {AzureObjectId}",
                    azureObjectId);
            }
        }

        logger.LogInformation(
            "Dispatched technical documentation notification {DocumentationId} to {RecipientCount} users",
            payload.DocumentationId, azureObjectIds.Count);
    }

    private async Task<HashSet<Guid>> GetRecipientUserIdsAsync(
        TechnicalDocumentationProcessingResultDto payload,
        CancellationToken cancellationToken)
    {
        HashSet<Guid> userIds = new();

        IEnumerable<ProjectMemberModulePermission> modulePermissions = await modulePermissionRepository.GetBySearch(
            p => p.TenantId == payload.TenantId
                && p.ProjectId == payload.ProjectId
                && p.Module == ProjectModule.TechnicalDocumentation);

        foreach (ProjectMemberModulePermission permission in modulePermissions)
        {
            userIds.Add(permission.UserId);
        }

        IEnumerable<ProjectMember> admins = await projectMemberRepository.GetBySearch(
            m => m.TenantId == payload.TenantId
                && m.ProjectId == payload.ProjectId
                && m.IsAdmin
                && m.IsActive);

        foreach (ProjectMember admin in admins)
        {
            userIds.Add(admin.UserId);
        }

        return userIds;
    }
}
