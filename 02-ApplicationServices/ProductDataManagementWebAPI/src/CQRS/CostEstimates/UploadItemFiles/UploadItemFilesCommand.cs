using Business.Interfaces.Constants;
using Microsoft.AspNetCore.Http;

namespace CQRS.CostEstimates.UploadItemFiles
{
    /// <summary>
    /// Dodaje pliki do pozycji kosztorysu (append).
    /// Nie wymaga fieldDefinitionId — pliki są przypisane bezpośrednio do pozycji (CostEstimateItemFile).
    /// Dozwolone formaty: PDF, JPG. Max rozmiar pliku: 50 MB. Max 10 plików na raz.
    /// </summary>
    public sealed record UploadItemFilesCommand : CostEstimateCommandBase, IRequestCommand<List<Guid>>
    {
        /// <summary>
        /// ID pozycji kosztorysu (CostEstimateItem), do której dołączane są pliki.
        /// </summary>
        public Guid ItemId { get; init; }

        /// <summary>
        /// Lista plików do dodania (PDF, JPG — max 50 MB każdy).
        /// </summary>
        public List<IFormFile> Files { get; init; } = new();

        public override string PermissionCode => PermissionCodes.ProjectEstimates;
    }
}
