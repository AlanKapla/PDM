using Business.Interfaces.Model;
using Entities.Models.Files;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.CreateDirectory
{
    public sealed class CreateDirectoryCommandHandler : IRequestHandler<CreateDirectoryCommand, Unit>
    {
        private readonly IRepository<ProjectFilePackage> projectFilePackageRepo;
        private readonly ICurrentUser currentUser;

        public CreateDirectoryCommandHandler(
            IRepository<ProjectFilePackage> projectFilePackageRepo,
            ICurrentUser currentUser)
        {
            this.projectFilePackageRepo = projectFilePackageRepo;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(CreateDirectoryCommand request, CancellationToken cancellationToken)
        {
            ProjectFilePackage directory = new ProjectFilePackage
            {
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                Name = request.DirectoryName,
                ParentId = request.ParentId,
                OwnerId = currentUser.Id,
                CreatedByUserId = currentUser.Id,
                CreatedAt = DateTime.UtcNow
            };

            await projectFilePackageRepo.Insert(directory);

            return Unit.Value;
        }
    }
}
