using MediatR;

namespace CQRS.Tenants.ToggleTenantStatus;

/// <summary>
/// Command do zmiany statusu aktywności tenanta
/// </summary>
public record ToggleTenantStatusCommand(Guid TenantId, bool IsActive) : IRequestCommand<Unit>;
