namespace Business.Interfaces.Model;

public record ResourceRef(
    Guid TenantId,
    Guid? ProjectId = null,
    Guid? ResourceId = null
);
