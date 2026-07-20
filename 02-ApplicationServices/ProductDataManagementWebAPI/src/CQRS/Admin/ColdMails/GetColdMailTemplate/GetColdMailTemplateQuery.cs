using Business.Interfaces.WebModels.Admin;
using CQRS;

namespace CQRS.Admin.ColdMails.GetColdMailTemplate;

public sealed record GetColdMailTemplateQuery
    : IRequestQuery<ColdMailTemplateWeb>;
