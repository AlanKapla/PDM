using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using CQRS.TechnicalDocumentation.CreateTechnicalDocumentation;
using CQRS.TechnicalDocumentation.GetTechnicalDocumentationCount;
using CQRS.TechnicalDocumentation.GetTechnicalDocumentationDetails;
using CQRS.TechnicalDocumentation.GetTechnicalDocumentationList;
using CQRS.TechnicalDocumentation.RetryTechnicalDocumentation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/tenants/{tenantId:guid}/projects/{projectId:guid}/technical-documentation")]
public sealed class TechnicalDocumentationController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    [Authorize(Policy = PermissionCodes.ProjectTechnicalDocumentation)]
    [ProducesResponseType(typeof(List<TechnicalDocumentationListItemWeb>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid projectId,
        CancellationToken cancellationToken)
    {
        GetTechnicalDocumentationListQuery query = new(tenantId, projectId);
        List<TechnicalDocumentationListItemWeb> result = await Send(query);
        return Ok(result);
    }

    [HttpGet("count")]
    [Authorize(Policy = PermissionCodes.ProjectTechnicalDocumentation)]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCount(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid projectId,
        CancellationToken cancellationToken)
    {
        GetTechnicalDocumentationCountQuery query = new(tenantId, projectId);
        int result = await Send(query);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCodes.ProjectTechnicalDocumentation)]
    [ProducesResponseType(typeof(TechnicalDocumentationDetailsWeb), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetails(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid projectId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        GetTechnicalDocumentationDetailsQuery query = new(tenantId, projectId, id);
        TechnicalDocumentationDetailsWeb result = await Send(query);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.ProjectTechnicalDocumentation)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(52_428_800)]
    [RequestFormLimits(MultipartBodyLengthLimit = 52_428_800)]
    [ProducesResponseType(typeof(TechnicalDocumentationCreatedWeb), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid projectId,
        [FromForm] string name,
        [FromForm] string? description,
        [FromForm] List<IFormFile> files,
        CancellationToken cancellationToken)
    {
        CreateTechnicalDocumentationCommand command = new()
        {
            TenantId = tenantId,
            ProjectId = projectId,
            Name = name,
            Description = description,
            Files = files
        };

        TechnicalDocumentationCreatedWeb result = await Send(command);
        return AcceptedAtAction(
            nameof(GetDetails),
            new { tenantId, projectId, id = result.Id },
            result);
    }

    [HttpPost("{id:guid}/retry")]
    [Authorize(Policy = PermissionCodes.ProjectTechnicalDocumentation)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Retry(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid projectId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await Send(new RetryTechnicalDocumentationCommand(tenantId, projectId, id));
        return Accepted();
    }
}
