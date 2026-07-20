const PLACEHOLDER_SUBJECT = "Temat wiadomości";
const PLACEHOLDER_BODY =
  "Treść wiadomości pojawi się tutaj po wpisaniu w formularzu.";

function escapeHtml(value: string): string {
  return value
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#39;");
}

function looksLikeHtml(value: string): boolean {
  return /<\/?(p|div|h[1-6]|ul|ol|li|blockquote|strong|em|b|i|u|s|a|br)\b/i.test(
    value
  );
}

/** Lightweight sanitize for preview — mirrors server allowlist intent. */
function sanitizeBodyHtml(html: string): string {
  return html
    .replace(/<\/?(script|style|iframe|object|embed|form)[^>]*>/gi, "")
    .replace(/\son\w+\s*=\s*("[^"]*"|'[^']*'|[^\s>]+)/gi, "");
}

function formatBodyForHtml(body: string): string {
  const trimmed: string = body.trim();
  if (looksLikeHtml(trimmed)) {
    return sanitizeBodyHtml(trimmed);
  }

  const normalized: string = trimmed.replace(/\r\n/g, "\n");
  return escapeHtml(normalized).replace(/\n/g, "<br />");
}

/**
 * Fills server cold-mail.html placeholders the same way as ColdMailHtmlBuilder.Build.
 */
export function fillColdMailTemplate(
  htmlTemplate: string,
  appUrl: string,
  ctaLabel: string,
  subject: string,
  body: string
): string {
  const trimmedSubject: string = subject.trim();
  const trimmedBody: string = body.trim();

  const subjectHtml: string = escapeHtml(
    trimmedSubject.length > 0 ? trimmedSubject : PLACEHOLDER_SUBJECT
  );
  const bodyHtml: string =
    trimmedBody.length > 0
      ? formatBodyForHtml(trimmedBody)
      : escapeHtml(PLACEHOLDER_BODY);

  return htmlTemplate
    .split("{subject}")
    .join(subjectHtml)
    .split("{bodyText}")
    .join(bodyHtml)
    .split("{appUrl}")
    .join(appUrl)
    .split("{ctaLabel}")
    .join(ctaLabel);
}
