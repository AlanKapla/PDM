import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { axe } from "vitest-axe";
import { vi } from "vitest";
import { renderWithChakra } from "../../../test/render-with-chakra";
import { ColdMailTemplatePreview } from "../ColdMailTemplatePreview";

vi.mock("../../../hooks/useColdMailTemplate", () => ({
  useColdMailTemplate: () => ({
    data: {
      htmlTemplate:
        "<!DOCTYPE html><html><body><h1>{subject}</h1><p>{bodyText}</p><a href=\"{appUrl}\">{ctaLabel}</a></body></html>",
      appUrl: "https://brickly.pro",
      ctaLabel: "Poznaj Brickly",
    },
    isPending: false,
    isError: false,
    error: null,
  }),
}));

describe("ColdMailTemplatePreview — AXE", () => {
  it("brakNaruszen_zTrescia", async () => {
    const queryClient = new QueryClient();
    const { container } = renderWithChakra(
      <QueryClientProvider client={queryClient}>
        <ColdMailTemplatePreview
          subject="Oferta współpracy"
          body={"Dzień dobry,\n\nto treść wiadomości."}
        />
      </QueryClientProvider>
    );
    const results = await axe(container, { iframes: false });
    expect(results).toHaveNoViolations();
  });
});
