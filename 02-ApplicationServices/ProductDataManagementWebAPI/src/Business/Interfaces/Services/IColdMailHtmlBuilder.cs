using Business.Interfaces.WebModels.Admin;

namespace Business.Interfaces.Services;

public interface IColdMailHtmlBuilder
{
    /// <summary>
    /// Renders cold-mail.html with subject and body (HTML sanitized when rich content).
    /// </summary>
    string Build(string subject, string body);

    /// <summary>
    /// Strips HTML tags for the plain-text email alternative.
    /// </summary>
    string ToPlainText(string body);

    /// <summary>
    /// Returns the raw cold-mail.html template and fixed placeholders (appUrl, ctaLabel)
    /// for client-side live preview without per-keystroke API calls.
    /// </summary>
    ColdMailTemplateWeb GetTemplate();
}
