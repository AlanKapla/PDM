using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Microsoft.AspNetCore.Http;

namespace CQRS.CostEstimates.UploadCostEstimateFieldFiles
{
    /// <summary>
    /// Command to replace all files on a cost estimate item field of type ItemSystemFiles.
    /// Strategy: always deletes ALL existing files (DB soft-delete + blob delete), then uploads new ones.
    /// If the field value does not yet exist on the item, it will be created automatically.
    /// Sending empty Files list clears all files from the field.
    /// Allowed formats: PDF, JPG. Max file size: 50 MB.
    /// </summary>
    public sealed record UploadCostEstimateFieldFilesCommand : IRequestCommand<List<Guid>>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
        public Guid CostEstimateId { get; init; }

        /// <summary>
        /// ID pozycji kosztorysu (CostEstimateItem) do której dołączane są pliki
        /// </summary>
        public Guid ItemId { get; init; }

        /// <summary>
        /// ID definicji pola z szablonu (CostEstimateTemplateFieldDefinitionBase) typu ItemSystemFiles.
        /// Jeśli pozycja nie ma jeszcze wartości tego pola, zostanie automatycznie utworzona.
        /// </summary>
        public Guid FieldDefinitionId { get; init; }

        /// <summary>
        /// Nowa lista plików (PDF, JPG - max 50 MB każdy).
        /// Zastępuje wszystkie istniejące pliki — stare są usuwane z DB i Blob Storage.
        /// Pusta lista = usunięcie wszystkich plików z pola.
        /// </summary>
        public List<IFormFile> Files { get; init; } = new();

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
