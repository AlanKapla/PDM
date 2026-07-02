using Business.Interfaces.WebModels.TechnicalDocumentation;
using Entities.Models.TechnicalDocumentation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.TechnicalDocumentation.GetTechnicalDocumentationList;

public sealed class GetTechnicalDocumentationListQueryHandler
    : IRequestHandler<GetTechnicalDocumentationListQuery, List<TechnicalDocumentationListItemWeb>>
{
    private readonly IReadRepository<ProjectTechnicalDocumentation> documentationRepository;

    public GetTechnicalDocumentationListQueryHandler(
        IReadRepository<ProjectTechnicalDocumentation> documentationRepository)
    {
        this.documentationRepository = documentationRepository;
    }

    public async Task<List<TechnicalDocumentationListItemWeb>> Handle(
        GetTechnicalDocumentationListQuery request,
        CancellationToken cancellationToken)
    {
        List<ProjectTechnicalDocumentation> documentations = (await documentationRepository.GetBySearch(
            d => d.TenantId == request.TenantId && d.ProjectId == request.ProjectId,
            q => q.Include(d => d.Files))).ToList();

        return documentations
            .OrderByDescending(d => d.CreatedAt)
            .Select(MapToListItem)
            .ToList();
    }

    private static TechnicalDocumentationListItemWeb MapToListItem(ProjectTechnicalDocumentation documentation) =>
        new()
        {
            Id = documentation.Id,
            ProjectId = documentation.ProjectId,
            Name = documentation.Name,
            Description = documentation.Description,
            Status = documentation.Status,
            FileCount = documentation.Files.Count,
            CreatedAt = documentation.CreatedAt,
            CompletedAt = documentation.CompletedAt,
            ErrorMessage = documentation.ErrorMessage
        };
}
