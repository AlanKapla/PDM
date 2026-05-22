using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.CostEstimates;
using CQRS.CostEstimates;
using CQRS.CostEstimates.AddCostEstimateGroup;
using CQRS.CostEstimates.AddCostEstimateItem;
using CQRS.CostEstimates.CopyCostEstimate;
using CQRS.CostEstimates.CreateCostEstimate;
using CQRS.CostEstimates.DeleteCostEstimate;
using CQRS.CostEstimates.DeleteCostEstimateGroup;
using CQRS.CostEstimates.DeleteCostEstimateItem;
using CQRS.CostEstimates.GetCostEstimateDetails;
using CQRS.CostEstimates.GetCostEstimates;
using CQRS.CostEstimates.MoveCostEstimateItem;
using CQRS.CostEstimates.RecalculateCostEstimate;
using CQRS.CostEstimates.ReorderCostEstimateGroups;
using CQRS.CostEstimates.ReorderCostEstimateItems;
using CQRS.CostEstimates.ShareCostEstimate;
using CQRS.CostEstimates.UpdateCostEstimate;
using CQRS.CostEstimates.UpdateCostEstimateShares;
using CQRS.CostEstimates.UploadCostEstimateFieldFiles;
using CQRS.CostEstimates.UpsertCostEstimateGroupField;
using CQRS.CostEstimates.UpsertCostEstimateItemField;
using Entities.Models.CostEstimates;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebApi.Controllers;

namespace WebApi.Tests.Controllers
{
    public class CostEstimateControllerTests : ControllerTestBase
    {
        private readonly CostEstimateController sut;

        public CostEstimateControllerTests()
        {
            sut = new CostEstimateController(MediatorMock.Object);
        }

        [Fact]
        public async Task GetCostEstimates_ReturnsOk_WithScope()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();

            IActionResult result = await sut.GetCostEstimates(tenantId, projectId, ResourceScope.Mine);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetCostEstimatesQuery>(q =>
                q.TenantId == tenantId && q.ProjectId == projectId && q.Scope == ResourceScope.Mine);
        }

        [Fact]
        public async Task GetCostEstimateDetails_ReturnsOk_WithIds()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid id = Guid.NewGuid();

            IActionResult result = await sut.GetCostEstimateDetails(tenantId, projectId, id);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetCostEstimateDetailsQuery>(q =>
                q.TenantId == tenantId && q.ProjectId == projectId && q.CostEstimateId == id);
        }

        [Fact]
        public async Task CreateCostEstimate_OverridesIds_AndReturnsCreated()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            CreateCostEstimateCommand command = new CreateCostEstimateCommand { TemplateId = Guid.NewGuid(), Name = "CE1" };

            IActionResult result = await sut.CreateCostEstimate(tenantId, projectId, command);

            result.Should().BeOfType<CreatedAtActionResult>();
            VerifyMediatorCalledOnce<CreateCostEstimateCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.Name == "CE1");
        }

        [Fact]
        public async Task UpdateCostEstimate_OverridesIds_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid id = Guid.NewGuid();
            UpdateCostEstimateCommand command = new UpdateCostEstimateCommand { Name = "X" };

            IActionResult result = await sut.UpdateCostEstimate(tenantId, projectId, id, command);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<UpdateCostEstimateCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.CostEstimateId == id);
        }

        [Fact]
        public async Task DeleteCostEstimate_BuildsCommand_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid id = Guid.NewGuid();

            IActionResult result = await sut.DeleteCostEstimate(tenantId, projectId, id);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<DeleteCostEstimateCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.CostEstimateId == id);
        }

        [Fact]
        public async Task CopyCostEstimate_OverridesIds_AndReturnsOk()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid id = Guid.NewGuid();
            CopyCostEstimateCommand command = new CopyCostEstimateCommand { TargetProjectIds = new List<Guid>() };

            IActionResult result = await sut.CopyCostEstimate(tenantId, projectId, id, command);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<CopyCostEstimateCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.CostEstimateId == id);
        }

        [Fact]
        public async Task UploadCostEstimateFieldFiles_BuildsCommand_AndReturnsOk()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid id = Guid.NewGuid();
            Guid itemId = Guid.NewGuid();
            Guid fieldDefinitionId = Guid.NewGuid();
            List<IFormFile> files = new List<IFormFile>();

            IActionResult result = await sut.UploadCostEstimateFieldFiles(tenantId, projectId, id, itemId, fieldDefinitionId, files);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<UploadCostEstimateFieldFilesCommand>(c =>
                c.TenantId == tenantId
                && c.ProjectId == projectId
                && c.CostEstimateId == id
                && c.ItemId == itemId
                && c.FieldDefinitionId == fieldDefinitionId);
        }

        [Fact]
        public async Task AddCostEstimateGroup_OverridesIds_AndReturnsCreated()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid id = Guid.NewGuid();
            AddCostEstimateGroupCommand command = new AddCostEstimateGroupCommand { Order = 0 };

            IActionResult result = await sut.AddCostEstimateGroup(tenantId, projectId, id, command);

            result.Should().BeOfType<CreatedAtActionResult>();
            VerifyMediatorCalledOnce<AddCostEstimateGroupCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.CostEstimateId == id);
        }

        [Fact]
        public async Task DeleteCostEstimateGroup_BuildsCommand_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid id = Guid.NewGuid();
            Guid groupId = Guid.NewGuid();

            IActionResult result = await sut.DeleteCostEstimateGroup(tenantId, projectId, id, groupId);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<DeleteCostEstimateGroupCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.CostEstimateId == id && c.GroupId == groupId);
        }

        [Fact]
        public async Task ReorderCostEstimateGroups_OverridesIds_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid id = Guid.NewGuid();
            ReorderCostEstimateGroupsCommand command = new ReorderCostEstimateGroupsCommand();

            IActionResult result = await sut.ReorderCostEstimateGroups(tenantId, projectId, id, command);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<ReorderCostEstimateGroupsCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.CostEstimateId == id);
        }

        [Fact]
        public async Task AddCostEstimateItem_OverridesIds_AndReturnsCreated()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid id = Guid.NewGuid();
            AddCostEstimateItemCommand command = new AddCostEstimateItemCommand { RelationType = ItemRelationType.None };

            IActionResult result = await sut.AddCostEstimateItem(tenantId, projectId, id, command);

            result.Should().BeOfType<CreatedAtActionResult>();
            VerifyMediatorCalledOnce<AddCostEstimateItemCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.CostEstimateId == id);
        }

        [Fact]
        public async Task DeleteCostEstimateItem_BuildsCommand_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid id = Guid.NewGuid();
            Guid itemId = Guid.NewGuid();

            IActionResult result = await sut.DeleteCostEstimateItem(tenantId, projectId, id, itemId);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<DeleteCostEstimateItemCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.CostEstimateId == id && c.ItemId == itemId);
        }

        [Fact]
        public async Task ReorderCostEstimateItems_OverridesIds_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid id = Guid.NewGuid();
            Guid groupId = Guid.NewGuid();
            ReorderCostEstimateItemsCommand command = new ReorderCostEstimateItemsCommand();

            IActionResult result = await sut.ReorderCostEstimateItems(tenantId, projectId, id, groupId, command);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<ReorderCostEstimateItemsCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.CostEstimateId == id && c.GroupId == groupId);
        }

        [Fact]
        public async Task MoveCostEstimateItem_OverridesIds_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid id = Guid.NewGuid();
            Guid itemId = Guid.NewGuid();
            Guid targetGroupId = Guid.NewGuid();
            MoveCostEstimateItemCommand command = new MoveCostEstimateItemCommand { TargetGroupId = targetGroupId };

            IActionResult result = await sut.MoveCostEstimateItem(tenantId, projectId, id, itemId, command);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<MoveCostEstimateItemCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.CostEstimateId == id && c.ItemId == itemId);
        }

        [Fact]
        public async Task RecalculateCostEstimate_BuildsCommand_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid id = Guid.NewGuid();

            IActionResult result = await sut.RecalculateCostEstimate(tenantId, projectId, id);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<RecalculateCostEstimateCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.CostEstimateId == id);
        }

        [Fact]
        public async Task UpsertCostEstimateGroupField_OverridesIds_AndReturnsOk()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid id = Guid.NewGuid();
            Guid groupId = Guid.NewGuid();
            UpsertCostEstimateGroupFieldCommand command = new UpsertCostEstimateGroupFieldCommand();

            IActionResult result = await sut.UpsertCostEstimateGroupField(tenantId, projectId, id, groupId, command);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<UpsertCostEstimateGroupFieldCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.CostEstimateId == id && c.GroupId == groupId);
        }

        [Fact]
        public async Task UpsertCostEstimateItemField_OverridesIds_AndReturnsOk()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid id = Guid.NewGuid();
            Guid itemId = Guid.NewGuid();
            UpsertCostEstimateItemFieldCommand command = new UpsertCostEstimateItemFieldCommand();

            IActionResult result = await sut.UpsertCostEstimateItemField(tenantId, projectId, id, itemId, command);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<UpsertCostEstimateItemFieldCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.CostEstimateId == id && c.ItemId == itemId);
        }

        [Fact]
        public async Task ShareCostEstimate_BuildsCommand_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid id = Guid.NewGuid();
            ShareCostEstimateRequestWeb body = new ShareCostEstimateRequestWeb(new List<Guid>());

            IActionResult result = await sut.ShareCostEstimate(tenantId, projectId, id, body);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<ShareCostEstimateCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.CostEstimateId == id);
        }

        [Fact]
        public async Task UpdateCostEstimateShares_BuildsCommand_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid id = Guid.NewGuid();
            UpdateCostEstimateSharesRequestWeb body = new UpdateCostEstimateSharesRequestWeb(new List<Guid>());

            IActionResult result = await sut.UpdateCostEstimateShares(tenantId, projectId, id, body);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<UpdateCostEstimateSharesCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.CostEstimateId == id);
        }
    }
}
