namespace Business.Interfaces.WebModels.Admin;

public sealed record ColdMailTemplateWeb(
    string HtmlTemplate,
    string AppUrl,
    string CtaLabel);
