using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.Contractors;
using CQRS.Contractors.CreateContractor;
using CQRS.Contractors.DeleteContractor;
using CQRS.Contractors.GetContractor;
using CQRS.Contractors.GetContractors;
using CQRS.Contractors.UpdateContractor;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("api/tenants/{tenantId:guid}/contractors")]
    [ApiController]
    public class ContractorController(IMediator mediator) : BaseApiController(mediator)
    {
        [HttpGet]
        [Authorize(Policy = PermissionCodes.TenantView)]
        [ProducesResponseType(typeof(IEnumerable<ContractorWeb>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetContractors(
            [FromRoute] Guid tenantId,
            [FromQuery] string? search,
            CancellationToken cancellationToken)
        {
            GetContractorsQuery query = new GetContractorsQuery { TenantId = tenantId, Search = search };
            IEnumerable<ContractorWeb> result = await Send(query);
            return Ok(result);
        }

        [HttpGet("{contractorId:guid}")]
        [Authorize(Policy = PermissionCodes.TenantView)]
        [ProducesResponseType(typeof(ContractorWeb), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetContractor(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid contractorId,
            CancellationToken cancellationToken)
        {
            GetContractorQuery query = new GetContractorQuery { TenantId = tenantId, ContractorId = contractorId };
            ContractorWeb result = await Send(query);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = PermissionCodes.TenantEdit)]
        [ProducesResponseType(typeof(ContractorWeb), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateContractor(
            [FromRoute] Guid tenantId,
            [FromBody] CreateContractorCommand command,
            CancellationToken cancellationToken)
        {
            command = command with { TenantId = tenantId };
            ContractorWeb result = await Send(command);
            return Created(string.Empty, result);
        }

        [HttpPut("{contractorId:guid}")]
        [Authorize(Policy = PermissionCodes.TenantEdit)]
        [ProducesResponseType(typeof(ContractorWeb), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateContractor(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid contractorId,
            [FromBody] UpdateContractorCommand command,
            CancellationToken cancellationToken)
        {
            command = command with { TenantId = tenantId, Id = contractorId };
            ContractorWeb result = await Send(command);
            return Ok(result);
        }

        [HttpDelete("{contractorId:guid}")]
        [Authorize(Policy = PermissionCodes.TenantEdit)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteContractor(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid contractorId,
            CancellationToken cancellationToken)
        {
            DeleteContractorCommand command = new DeleteContractorCommand
            {
                TenantId = tenantId,
                Id = contractorId,
            };
            await Send(command);
            return NoContent();
        }
    }
}
