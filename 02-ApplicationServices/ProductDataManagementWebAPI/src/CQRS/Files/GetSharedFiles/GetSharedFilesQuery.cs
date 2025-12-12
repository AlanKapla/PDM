using Business.Interfaces.WebModels.Files;

namespace CQRS.Files.GetSharedFiles
{
    /// <summary>
    /// Query do pobierania plików udostępnionych użytkownikowi, zgrupowanych po paczkach
    /// </summary>
    public record GetSharedFilesQuery : IRequestQuery<List<SharedProjectFilePackageWeb>>
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public GetSharedFilesQuery(Guid tenantId, Guid projectId)
        {
            TenantId = tenantId;
            ProjectId = projectId;
        }
    }
}
