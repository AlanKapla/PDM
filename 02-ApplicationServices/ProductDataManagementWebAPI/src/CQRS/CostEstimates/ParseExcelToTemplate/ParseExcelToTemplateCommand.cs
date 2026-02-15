using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.CostEstimateTemplates;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace CQRS.CostEstimates.ParseExcelToTemplate;

/// <summary>
/// Command to parse Excel file and generate Template structure (preview only)
/// Can accept either uploaded file OR file from database (by ID)
/// Returns template DTO for user review - does NOT save to database
/// </summary>
public sealed record ParseExcelToTemplateCommand : IRequestCommand<CostEstimateTemplateUpdateDto>, IAuthorizableRequest
{
    /// <summary>
    /// Excel file (if uploading directly)
    /// </summary>
    public IFormFile? ExcelFile { get; init; }

    /// <summary>
    /// File ID from database (if using previously uploaded file)
    /// </summary>
    public Guid? FileId { get; init; }

    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }

    public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
    
    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}

