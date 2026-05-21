using CQRS.CostEstimateTemplates.CreateCostEstimateTemplate;
using CQRS.CostEstimateTemplates.CreateCostEstimateTemplateFromDefault;
using CQRS.CostEstimateTemplates.DeleteCostEstimateTemplate;
using CQRS.CostEstimateTemplates.DuplicateCostEstimateTemplate;
using CQRS.CostEstimateTemplates.GetCostEstimateTemplateDetails;
using CQRS.CostEstimateTemplates.GetCostEstimateTemplates;
using CQRS.CostEstimateTemplates.GetDefaultCostEstimateTemplateDetails;
using CQRS.CostEstimateTemplates.GetDefaultCostEstimateTemplates;
using CQRS.CostEstimateTemplates.GetFieldTypeConfigurations;
using CQRS.CostEstimateTemplates.UpdateCostEstimateTemplate;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers;

namespace WebApi.Tests.Controllers
{
    public class CostEstimateTemplateControllerTests : ControllerTestBase
    {
        private readonly CostEstimateTemplateController sut;

        public CostEstimateTemplateControllerTests()
        {
            sut = new CostEstimateTemplateController(MediatorMock.Object);
        }

        [Fact]
        public async Task GetTemplates_ReturnsOk()
        {
            IActionResult result = await sut.GetTemplates();

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetCostEstimateTemplatesQuery>();
        }

        [Fact]
        public async Task GetFieldTypeConfigurations_ReturnsOk()
        {
            IActionResult result = await sut.GetFieldTypeConfigurations();

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetFieldTypeConfigurationsQuery>();
        }

        [Fact]
        public async Task GetTemplateDetails_PassesId_AndReturnsOk()
        {
            Guid id = Guid.NewGuid();

            IActionResult result = await sut.GetTemplateDetails(id);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetCostEstimateTemplateDetailsQuery>(q => q.TemplateId == id);
        }

        [Fact]
        public async Task CreateTemplate_ReturnsCreated()
        {
            CreateCostEstimateTemplateCommand command = new CreateCostEstimateTemplateCommand("Test", null);

            IActionResult result = await sut.CreateTemplate(command);

            result.Should().BeOfType<CreatedAtActionResult>();
            VerifyMediatorCalledOnce<CreateCostEstimateTemplateCommand>(c => c.Name == "Test");
        }

        [Fact]
        public async Task UpdateTemplate_WhenIdMatches_ReturnsNoContent()
        {
            Guid id = Guid.NewGuid();
            UpdateCostEstimateTemplateCommand command = new UpdateCostEstimateTemplateCommand(
                id, Guid.NewGuid(), "Name", null, null,
                false, false, null, false, null,
                false, null, null, null, null, null, null, null);

            IActionResult result = await sut.UpdateTemplate(id, command);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<UpdateCostEstimateTemplateCommand>(c => c.TemplateId == id);
        }

        [Fact]
        public async Task UpdateTemplate_WhenIdMismatch_ReturnsBadRequest()
        {
            Guid id = Guid.NewGuid();
            UpdateCostEstimateTemplateCommand command = new UpdateCostEstimateTemplateCommand(
                Guid.NewGuid(), Guid.NewGuid(), "Name", null, null,
                false, false, null, false, null,
                false, null, null, null, null, null, null, null);

            IActionResult result = await sut.UpdateTemplate(id, command);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task DeleteTemplate_PassesId_AndReturnsNoContent()
        {
            Guid id = Guid.NewGuid();

            IActionResult result = await sut.DeleteTemplate(id);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<DeleteCostEstimateTemplateCommand>(c => c.TemplateId == id);
        }

        [Fact]
        public async Task DuplicateTemplate_OverridesSourceId_AndReturnsCreated()
        {
            Guid id = Guid.NewGuid();
            DuplicateCostEstimateTemplateCommand command = new DuplicateCostEstimateTemplateCommand(Guid.Empty, "Copy", null);

            IActionResult result = await sut.DuplicateTemplate(id, command);

            result.Should().BeOfType<CreatedAtActionResult>();
            VerifyMediatorCalledOnce<DuplicateCostEstimateTemplateCommand>(c => c.SourceTemplateId == id);
        }

        [Fact]
        public async Task GetDefaultTemplates_ReturnsOk()
        {
            IActionResult result = await sut.GetDefaultTemplates();

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetDefaultCostEstimateTemplatesQuery>();
        }

        [Fact]
        public async Task GetDefaultTemplateDetails_PassesSlug_AndReturnsOk()
        {
            IActionResult result = await sut.GetDefaultTemplateDetails("basic");

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetDefaultCostEstimateTemplateDetailsQuery>(q => q.Slug == "basic");
        }

        [Fact]
        public async Task CreateTemplateFromDefault_OverridesSlug_AndReturnsCreated()
        {
            CreateCostEstimateTemplateFromDefaultCommand command = new CreateCostEstimateTemplateFromDefaultCommand("New", null);

            IActionResult result = await sut.CreateTemplateFromDefault("basic", command);

            result.Should().BeOfType<CreatedAtActionResult>();
            VerifyMediatorCalledOnce<CreateCostEstimateTemplateFromDefaultCommand>(c => c.Slug == "basic");
        }
    }
}
