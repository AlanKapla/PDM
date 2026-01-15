using CQRS;

namespace CQRS.Notifications.GetUnreadCounter
{
    public record GetUnreadCounterQuery() : IRequestQuery<int>;
}
