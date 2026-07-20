import { describe, expect, it } from "vitest";
import { fillColdMailTemplate } from "./fillColdMailTemplate";

describe("fillColdMailTemplate", () => {
  const template =
    "<html><title>{subject}</title><body>{subject}|{bodyText}|{appUrl}|{ctaLabel}</body></html>";

  it("wstawia temat i plain-text treść do szablonu", () => {
    const html: string = fillColdMailTemplate(
      template,
      "https://brickly.pro",
      "Poznaj Brickly",
      "Oferta",
      "Linia1\nLinia2"
    );

    expect(html).toContain("Oferta");
    expect(html).toContain("Linia1<br />Linia2");
    expect(html).toContain("https://brickly.pro");
    expect(html).toContain("Poznaj Brickly");
  });

  it("escapuje niebezpieczny HTML w temacie; plain-text body bez tagów", () => {
    const html: string = fillColdMailTemplate(
      template,
      "https://brickly.pro",
      "CTA",
      "<b>x</b>",
      "tekst & znaki < specjalne >"
    );

    expect(html).toContain("&lt;b&gt;x&lt;/b&gt;");
    expect(html).toContain("tekst &amp; znaki &lt; specjalne &gt;");
  });

  it("wstawia sformatowany HTML z edytora bez escapowania tagów", () => {
    const html: string = fillColdMailTemplate(
      template,
      "https://brickly.pro",
      "CTA",
      "Oferta",
      "<p>Cześć <strong>świecie</strong></p>"
    );

    expect(html).toContain("<p>Cześć <strong>świecie</strong></p>");
    expect(html).not.toContain("&lt;p&gt;");
  });

  it("usuwa skrypty z body HTML", () => {
    const html: string = fillColdMailTemplate(
      template,
      "https://brickly.pro",
      "CTA",
      "Oferta",
      "<p>ok</p><script>alert(1)</script>"
    );

    expect(html).toContain("<p>ok</p>");
    expect(html).not.toContain("<script>");
  });
});
