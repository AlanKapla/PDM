using Business.Interfaces.Constants;
using Microsoft.AspNetCore.Http;

namespace CQRS.CostEstimates.ReplaceItemFiles
{
    /// <summary>
    /// Zastępuje wszystkie pliki pozycji kosztorysu (replace all).
    /// Soft-delete wszystkich istniejących plików + usunięcie blobów, następnie upload nowych.
    /// Dozwolone formaty: PDF, JPG. Max rozmiar pliku: 50 MB. Max 10 plików na raz.
    /// </summary>
    public sealed record ReplaceItemFilesCommand : CostEstimateCommandBase, IRequestCommand<List<Guid>>
    {
        /// <summary>
        /// ID pozycji kosztorysu (CostEstimateItem), której pliki są zastępowane.
        /// </summary>
        public Guid ItemId { get; init; }

        /// <summary>
        /// Nowa lista plików (PDF, JPG — max 50 MB każdy).
        /// Zastępuje wszystkie istniejące pliki — stare są soft-deleted i usuwane z blob storage.
        /// Pusta lista = usunięcie wszystkich plików z pozycji.
        /// </summary>
        public List<IFormFile> Files { get; init; } = new();

        public override string PermissionCode => PermissionCodes.ProjectEstimates;
    }
}
