using Business.Interfaces.Model;
using Business.Interfaces.Services;
using MediatR;

namespace CQRS.CostEstimateTemplates.CreateCostEstimateTemplate
{
    /// <summary>
    /// Handler dla tworzenia szablonu kosztorysu.
    /// Tworzy tylko minimalny szablon (nazwa, opis).
    /// Cała struktura (pola, waluty, jednostki, konfiguracje) jest dodawana przez UpdateCostEstimateTemplate.
    /// </summary>
    public class CreateCostEstimateTemplateCommandHandler : IRequestHandler<CreateCostEstimateTemplateCommand, Guid>
    {
        private readonly ICostEstimateTemplateService templateService;
        private readonly ICurrentUser currentUser;

        public CreateCostEstimateTemplateCommandHandler(
            ICostEstimateTemplateService templateService,
            ICurrentUser currentUser)
        {
            this.templateService = templateService;
            this.currentUser = currentUser;
        }

        public async Task<Guid> Handle(CreateCostEstimateTemplateCommand request, CancellationToken cancellationToken)
        {
            return await templateService.CreateTemplateAsync(
                currentUser.Id,
                request.Name,
                request.Description,
                cancellationToken);
        }
    }
}
