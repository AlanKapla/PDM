import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { axe } from "vitest-axe";
import { vi } from "vitest";
import { renderWithChakra } from "../../../test/render-with-chakra";
import { ColdMailSendForm } from "../ColdMailSendForm";
import { ColdMailBodyEditor } from "../ColdMailBodyEditor";

vi.mock("../../../hooks/useColdMailTemplate", () => ({
  useColdMailTemplate: () => ({
    data: {
      htmlTemplate:
        "<html><body><h1>{subject}</h1><div>{bodyText}</div></body></html>",
      appUrl: "https://brickly.pro",
      ctaLabel: "Poznaj Brickly",
    },
    isPending: false,
    isError: false,
    error: null,
  }),
}));

describe("ColdMailSendForm — AXE", () => {
  it("brakNaruszen_pustyFormularz", async () => {
    const queryClient = new QueryClient();
    const { container } = renderWithChakra(
      <QueryClientProvider client={queryClient}>
        <ColdMailSendForm
          onSubmit={async () => undefined}
          isSubmitting={false}
        />
      </QueryClientProvider>
    );
    const results = await axe(container, { iframes: false });
    expect(results).toHaveNoViolations();
  });
});

describe("ColdMailBodyEditor — AXE", () => {
  it("brakNaruszen_pustyEdytor", async () => {
    const { container } = renderWithChakra(
      <ColdMailBodyEditor value="" onChange={() => undefined} />
    );
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
