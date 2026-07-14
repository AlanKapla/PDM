using System.Text.Json.Serialization;

namespace Entities.Models.Costs
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CostApprovalStatus
    {
        Draft = 0,
        PendingApproval = 1,
        Approved = 2
    }
}
