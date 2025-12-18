using MediatR;

namespace CQRS.Projects.ToggleProjectStatus;

/// <summary>
/// Command do zmiany statusu aktywności projektu
/// </summary>
public record ToggleProjectStatusCommand(Guid TenantId, Guid ProjectId, bool IsActive) : IRequestCommand<Unit>;
