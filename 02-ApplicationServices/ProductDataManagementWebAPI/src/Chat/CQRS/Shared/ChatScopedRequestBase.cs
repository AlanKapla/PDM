using Business.Interfaces.Model;
using CQRS;

namespace Chat.CQRS.Shared;

/// <summary>
/// Base record for chat-scoped Commands/Queries that operate within a tenant
/// (and optionally a project) context. Carries TenantId/ProjectId/ChatId for
/// the AuthorizationBehavior pipeline to resolve the resource and the
/// PermissionCode required for the operation. Per-chat membership is verified
/// independently by handlers (defense in depth).
/// </summary>
public abstract record ChatScopedRequestBase : IAuthorizableRequest
{
    public Guid TenantId { get; init; }
    public Guid? ProjectId { get; init; }
    public Guid ChatId { get; init; }

    public abstract string PermissionCode { get; }

    public virtual ResourceRef GetResource() =>
        new ResourceRef(TenantId: TenantId, ProjectId: ProjectId);
}
