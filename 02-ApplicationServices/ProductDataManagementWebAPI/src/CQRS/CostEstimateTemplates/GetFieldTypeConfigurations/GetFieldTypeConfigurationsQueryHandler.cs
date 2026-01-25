using Business.Implementation.Helpers;
using Business.Interfaces.WebModels.CostEstimateTemplates;
using MediatR;

namespace CQRS.CostEstimateTemplates.GetFieldTypeConfigurations
{
    /// <summary>
    /// Handler zwracający konfigurację wszystkich dostępnych typów pól
    /// Publiczny endpoint - nie wymaga autoryzacji specyficznej (tylko zalogowany użytkownik)
    /// </summary>
    public class GetFieldTypeConfigurationsQueryHandler : IRequestHandler<GetFieldTypeConfigurationsQuery, Dictionary<int, CostEstimateFieldTypeConfigWeb[]>>
    {
        public Task<Dictionary<int, CostEstimateFieldTypeConfigWeb[]>> Handle(GetFieldTypeConfigurationsQuery request, CancellationToken cancellationToken)
        {
            var configurations = CostEstimateFieldTypeHelper.FieldTypeConfigurations;
            return Task.FromResult(configurations);
        }
    }
}
