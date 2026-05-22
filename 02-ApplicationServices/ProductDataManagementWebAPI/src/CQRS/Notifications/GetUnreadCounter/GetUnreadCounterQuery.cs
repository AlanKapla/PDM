using CQRS;

namespace CQRS.Notifications.GetUnreadCounter
{
    public sealed record GetUnreadCounterQuery : IRequestQuery<int>;
}
