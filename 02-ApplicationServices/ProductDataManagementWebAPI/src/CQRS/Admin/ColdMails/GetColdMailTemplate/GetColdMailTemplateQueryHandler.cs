using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Admin;
using MediatR;

namespace CQRS.Admin.ColdMails.GetColdMailTemplate;

public sealed class GetColdMailTemplateQueryHandler
    : IRequestHandler<GetColdMailTemplateQuery, ColdMailTemplateWeb>
{
    private readonly IColdMailHtmlBuilder coldMailHtmlBuilder;
    private readonly ICurrentUser currentUser;

    public GetColdMailTemplateQueryHandler(
        IColdMailHtmlBuilder coldMailHtmlBuilder,
        ICurrentUser currentUser)
    {
        this.coldMailHtmlBuilder = coldMailHtmlBuilder;
        this.currentUser = currentUser;
    }

    public Task<ColdMailTemplateWeb> Handle(
        GetColdMailTemplateQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsSuperAdmin)
        {
            throw new ForbiddenApiException("Only SuperAdmin can load cold mail template.");
        }

        return Task.FromResult(coldMailHtmlBuilder.GetTemplate());
    }
}
