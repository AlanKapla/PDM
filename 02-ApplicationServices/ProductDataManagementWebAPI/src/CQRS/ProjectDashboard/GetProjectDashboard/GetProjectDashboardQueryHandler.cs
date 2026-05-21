using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.ProjectDashboard;
using Entities.Models.Projects;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectDashboard.GetProjectDashboard
{
    public sealed class GetProjectDashboardQueryHandler
        : IRequestHandler<GetProjectDashboardQuery, ProjectDashboardWeb>
    {
        private readonly IReadRepository<Project> projectRepository;
        private readonly IDashboardDataLoader dataLoader;
        private readonly IProjectDashboardAssembler assembler;
        private readonly ICurrentUser currentUser;

        public GetProjectDashboardQueryHandler(
            IReadRepository<Project> projectRepository,
            IDashboardDataLoader dataLoader,
            IProjectDashboardAssembler assembler,
            ICurrentUser currentUser)
        {
            this.projectRepository = projectRepository;
            this.dataLoader = dataLoader;
            this.assembler = assembler;
            this.currentUser = currentUser;
        }

        public async Task<ProjectDashboardWeb> Handle(
            GetProjectDashboardQuery request,
            CancellationToken cancellationToken)
        {
            if (!await currentUser.IsTenantOrProjectAdminAsync(request.TenantId, request.ProjectId, cancellationToken))
            {
                throw new ForbiddenApiException("User does not have access to this resource.");
            }

            Project project = await projectRepository.GetFirstBySearch(
                p => p.Id == request.ProjectId && p.TenantId == request.TenantId,
                cancellationToken)
                ?? throw new NotFoundApiException(nameof(Project), request.ProjectId.ToString());

            DashboardData data = await dataLoader.LoadAsync(request.TenantId, request.ProjectId, cancellationToken);

            return await assembler.AssembleAsync(project, data, cancellationToken);
        }
    }
}
