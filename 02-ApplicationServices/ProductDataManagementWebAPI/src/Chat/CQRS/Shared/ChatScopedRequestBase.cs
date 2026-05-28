namespace Chat.CQRS.Shared;

/// <summary>
/// Base record for chat-scoped Commands/Queries that operate within a tenant
/// (and optionally a project) context. Carries TenantId/ProjectId/ChatId for
/// handlers to resolve the resource. Per-chat membership is verified by handlers.
/// </summary>
public abstract record ChatScopedRequestBase
{
    public Guid TenantId { get; init; }
    public Guid? ProjectId { get; init; }
    public Guid ChatId { get; init; }
}
