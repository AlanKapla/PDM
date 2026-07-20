using Business.Interfaces.WebModels.Admin;
using CQRS;

namespace CQRS.Admin.ColdMails.SendColdMails
{
    public sealed record SendColdMailsCommand : IRequestCommand<SendColdMailsResultWeb>
    {
        public required IReadOnlyList<string> Emails { get; init; }
        public required string Subject { get; init; }
        public required string Body { get; init; }
    }
}
