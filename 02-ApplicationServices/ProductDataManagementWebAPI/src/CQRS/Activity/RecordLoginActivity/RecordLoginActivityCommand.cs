using CQRS;
using MediatR;

namespace CQRS.Activity.RecordLoginActivity
{
    public sealed record RecordLoginActivityCommand : IRequestCommand<Unit>
    {
        public required string IpAddress { get; init; }
        public string? Route { get; init; }
    }
}
