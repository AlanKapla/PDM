using CQRS;
using MediatR;

namespace CQRS.Activity.RecordDemoActivity
{
    public sealed record RecordDemoActivityCommand : IRequestCommand<Unit>
    {
        public required string IpAddress { get; init; }
        public string? Route { get; init; }
    }
}
