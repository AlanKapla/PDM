using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.CostEstimates;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace CQRS.CostEstimates.ParseExcelToCostEstimate;

/// <summary>
/// Command to parse Excel file with existing template and generate CostEstimate structure
/// Returns preview DTO - does NOT save to database
/// </summary>
public sealed record ParseExcelToCostEstimateCommand : IRequestCommand<CostEstimateUpdateDto>, IAuthorizableRequest
{
    /// <summary>
    /// Excel file (if uploading directly)
    /// </summary>
    public IFormFile? ExcelFile { get; init; }

    /// <summary>
    /// File ID from database (if using previously uploaded file)
    /// </summary>
    public Guid? FileId { get; init; }

    /// <summary>
    /// Template ID to use for mapping Excel data
    /// </summary>
    public Guid TemplateId { get; init; }

    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }

    public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
    
    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}

