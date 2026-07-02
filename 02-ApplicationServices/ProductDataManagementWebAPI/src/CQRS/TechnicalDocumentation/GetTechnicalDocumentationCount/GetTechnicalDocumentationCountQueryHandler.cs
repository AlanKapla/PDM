using Entities.Models.TechnicalDocumentation;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.TechnicalDocumentation.GetTechnicalDocumentationCount;

public sealed class GetTechnicalDocumentationCountQueryHandler
    : IRequestHandler<GetTechnicalDocumentationCountQuery, int>
{
    private readonly IReadRepository<ProjectTechnicalDocumentation> documentationRepository;

    public GetTechnicalDocumentationCountQueryHandler(
        IReadRepository<ProjectTechnicalDocumentation> documentationRepository)
    {
        this.documentationRepository = documentationRepository;
    }

    public async Task<int> Handle(
        GetTechnicalDocumentationCountQuery request,
        CancellationToken cancellationToken)
    {
        return await documentationRepository.CountAsync(
            d => d.TenantId == request.TenantId && d.ProjectId == request.ProjectId,
            cancellationToken);
    }
}
