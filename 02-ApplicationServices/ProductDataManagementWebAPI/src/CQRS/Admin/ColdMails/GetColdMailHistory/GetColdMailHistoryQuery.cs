using Business.Interfaces.WebModels.Admin;
using CQRS;

namespace CQRS.Admin.ColdMails.GetColdMailHistory
{
    public sealed record GetColdMailHistoryQuery(string? Email)
        : IRequestQuery<IReadOnlyList<ColdMailHistoryWeb>>;
}
