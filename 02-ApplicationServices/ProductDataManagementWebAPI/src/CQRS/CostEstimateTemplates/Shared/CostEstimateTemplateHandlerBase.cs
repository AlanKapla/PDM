using Business.Interfaces.Exceptions;
using Entities.Models.CostEstimates;

namespace CQRS.CostEstimateTemplates.Shared
{
    public abstract class CostEstimateTemplateHandlerBase
    {
        private static readonly FieldType[] RequiredFieldTypes =
        [
            FieldType.GroupName,
            FieldType.ItemSystemName,
            FieldType.ItemCalculatedValueNet,
            FieldType.ItemCalculatedValueGross,
        ];

        /// <summary>
        /// Validates that all mandatory template fields are present in the provided collection.
        /// Throws <see cref="ValidationApiException"/> with a list of missing fields when any are absent.
        /// </summary>
        protected static void ValidateRequiredTemplateFields(IEnumerable<FieldType> presentFieldTypes)
        {
            List<FieldType> missingFields = RequiredFieldTypes
                .Where(required => !presentFieldTypes.Contains(required))
                .ToList();

            if (missingFields.Count > 0)
            {
                string missingFieldNames = string.Join(", ", missingFields.Select(f => f.ToString()));
                throw new ValidationApiException($"Template is missing required fields: {missingFieldNames}.");
            }
        }
    }
}
