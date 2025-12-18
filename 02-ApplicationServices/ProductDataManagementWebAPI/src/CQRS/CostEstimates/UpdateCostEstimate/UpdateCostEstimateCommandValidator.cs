using CQRS.CostEstimates.Validators;
using Business.Interfaces.Model;
using Entities.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Repositiories.Repository.Interfaces;

namespace CQRS.CostEstimates.UpdateCostEstimate
{
    /// <summary>
    /// Walidator dla UpdateCostEstimateCommand
    /// </summary>
    public class UpdateCostEstimateCommandValidator : AbstractValidator<UpdateCostEstimateCommand>
    {
        private readonly IReadRepository<CostEstimate> costEstimateRepository;
        private readonly ICurrentUser currentUser;

        public UpdateCostEstimateCommandValidator(
            IReadRepository<CostEstimate> costEstimateRepository,
            ICurrentUser currentUser)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.currentUser = currentUser;

            RuleFor(x => x.CostEstimateId)
                .NotEmpty().WithMessage("Cost estimate ID is required");

            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("Invalid status");

            // Configure common rules from base validator
            BaseCostEstimateValidator.ConfigureCommonRules(this);

            When(x => x.TotalNet.HasValue, () =>
            {
                RuleFor(x => x.TotalNet!.Value)
                    .GreaterThanOrEqualTo(0).WithMessage("TotalNet must be greater than or equal to 0");
            });

            When(x => x.TotalGross.HasValue, () =>
            {
                RuleFor(x => x.TotalGross!.Value)
                    .GreaterThanOrEqualTo(0).WithMessage("TotalGross must be greater than or equal to 0");
            });

            // Validate data against template structure
            When(x => x.Data != null, () =>
            {
                RuleFor(x => x)
                    .MustAsync(async (command, cancellationToken) =>
                    {
                        var costEstimate = await costEstimateRepository.GetFirstBySearch(
                            c => c.Id == command.CostEstimateId && 
                                 c.TenantId == currentUser.ActiveTenantId && 
                                 c.OwnerId == currentUser.Id &&
                                 !c.IsDeleted,
                            cancellationToken,
                            q => q.Include(c => c.Template));

                        if (costEstimate == null)
                        {
                            return false;
                        }

                        return BaseCostEstimateValidator.ValidateDataAgainstTemplate(
                            command.Data,
                            costEstimate.Template.TemplateStructure,
                            out _);
                    })
                    .WithMessage(command => GetSchemaValidationError(command));
            });
        }

        private string GetSchemaValidationError(UpdateCostEstimateCommand command)
        {
            var costEstimate = costEstimateRepository.GetFirstBySearch(
                c => c.Id == command.CostEstimateId && 
                     c.TenantId == currentUser.ActiveTenantId && 
                     c.OwnerId == currentUser.Id &&
                     !c.IsDeleted,
                CancellationToken.None,
                q => q.Include(c => c.Template)).Result;

            if (costEstimate == null)
            {
                return "Cost estimate not found";
            }

            BaseCostEstimateValidator.ValidateDataAgainstTemplate(
                command.Data,
                costEstimate.Template.TemplateStructure,
                out var errorMessage);

            return errorMessage;
        }
    }
}
