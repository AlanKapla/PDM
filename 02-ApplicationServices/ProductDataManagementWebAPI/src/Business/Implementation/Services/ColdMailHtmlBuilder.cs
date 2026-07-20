using System.Net;
using System.Text.RegularExpressions;
using Business.Implementation.Helpers;
using Business.Interfaces.Configurations;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Admin;
using Microsoft.Extensions.Options;

namespace Business.Implementation.Services;

public sealed class ColdMailHtmlBuilder : IColdMailHtmlBuilder
{
    private const string CtaLabel = "Poznaj Brickly";
    private const string TemplateName = "cold-mail.html";

    private static readonly Regex LooksLikeHtmlRegex = new(
        @"</?(p|div|h[1-6]|ul|ol|li|blockquote|strong|em|b|i|u|s|a|br)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DangerousTagRegex = new(
        @"</?(script|style|iframe|object|embed|form|link|meta|svg|math)[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex EventHandlerRegex = new(
        @"\son\w+\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex HtmlTagRegex = new(
        "<[^>]+>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex WhitespaceRegex = new(
        @"\s+",
        RegexOptions.Compiled);

    private readonly IOptions<FrontendSettings> frontendSettings;

    public ColdMailHtmlBuilder(IOptions<FrontendSettings> frontendSettings)
    {
        this.frontendSettings = frontendSettings;
    }

    public string Build(string subject, string body)
    {
        ColdMailTemplateWeb template = GetTemplate();
        string subjectHtml = WebUtility.HtmlEncode(subject);
        string bodyHtml = FormatBodyForHtml(body);

        return ApplyPlaceholders(
            template.HtmlTemplate,
            subjectHtml,
            bodyHtml,
            template.AppUrl,
            template.CtaLabel);
    }

    public ColdMailTemplateWeb GetTemplate()
    {
        string baseUrl = frontendSettings.Value.BaseUrl.TrimEnd('/');
        string homePath = frontendSettings.Value.HomePath.TrimStart('/');
        string appUrl = string.IsNullOrWhiteSpace(homePath)
            ? baseUrl
            : $"{baseUrl}/{homePath}";

        return new ColdMailTemplateWeb(
            HtmlTemplate: EmailTemplateLoader.LoadRaw(TemplateName),
            AppUrl: appUrl,
            CtaLabel: CtaLabel);
    }

    public string ToPlainText(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return string.Empty;
        }

        if (!LooksLikeHtml(body))
        {
            return body.Trim();
        }

        string withoutTags = HtmlTagRegex.Replace(body, " ");
        string decoded = WebUtility.HtmlDecode(withoutTags);
        return WhitespaceRegex.Replace(decoded, " ").Trim();
    }

    private static string ApplyPlaceholders(
        string htmlTemplate,
        string subjectHtml,
        string bodyHtml,
        string appUrl,
        string ctaLabel)
    {
        return htmlTemplate
            .Replace("{subject}", subjectHtml, StringComparison.Ordinal)
            .Replace("{bodyText}", bodyHtml, StringComparison.Ordinal)
            .Replace("{appUrl}", appUrl, StringComparison.Ordinal)
            .Replace("{ctaLabel}", ctaLabel, StringComparison.Ordinal);
    }

    private static string FormatBodyForHtml(string body)
    {
        if (LooksLikeHtml(body))
        {
            return SanitizeRichHtml(body);
        }

        string normalized = body.Replace("\r\n", "\n", StringComparison.Ordinal);
        string encoded = WebUtility.HtmlEncode(normalized);
        return encoded.Replace("\n", "<br />", StringComparison.Ordinal);
    }

    private static string SanitizeRichHtml(string html)
    {
        string withoutDangerousTags = DangerousTagRegex.Replace(html, string.Empty);
        return EventHandlerRegex.Replace(withoutDangerousTags, string.Empty);
    }

    private static bool LooksLikeHtml(string body)
    {
        return LooksLikeHtmlRegex.IsMatch(body);
    }
}
