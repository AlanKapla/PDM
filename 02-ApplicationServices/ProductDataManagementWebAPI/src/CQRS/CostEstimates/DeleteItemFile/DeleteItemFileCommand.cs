using Business.Interfaces.Constants;
using MediatR;

namespace CQRS.CostEstimates.DeleteItemFile
{
    /// <summary>
    /// Usuwa pojedynczy plik z pozycji kosztorysu (soft delete + usuniecie bloba).
    /// </summary>
    public sealed record DeleteItemFileCommand : CostEstimateCommandBase, IRequestCommand<Unit>
    {
        public Guid ItemId { get; init; }
        public Guid FileId { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectEstimates;
    }
}
