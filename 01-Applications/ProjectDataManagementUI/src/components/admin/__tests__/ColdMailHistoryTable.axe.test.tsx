import { axe } from "vitest-axe";
import { renderWithChakra } from "../../../test/render-with-chakra";
import { ColdMailHistoryTable } from "../ColdMailHistoryTable";
import type { ColdMailHistoryWeb } from "../../../types/admin.types";

const sampleItems: ColdMailHistoryWeb[] = [
  {
    id: "1",
    batchId: "batch-1",
    recipientEmail: "prospect@example.com",
    subject: "Oferta współpracy",
    body: "Dzień dobry,\n\nto treść wiadomości.",
    htmlBody:
      "<!DOCTYPE html><html><body><h1>Oferta współpracy</h1><p>Dzień dobry,</p><p>to treść wiadomości.</p></body></html>",
    status: "Queued",
    errorMessage: null,
    sentByUserId: "user-1",
    sentAt: "2026-07-16T10:00:00Z",
  },
  {
    id: "2",
    batchId: "batch-1",
    recipientEmail: "failed@example.com",
    subject: "Oferta współpracy",
    body: "Treść",
    htmlBody: "<!DOCTYPE html><html><body><p>Treść</p></body></html>",
    status: "Failed",
    errorMessage: "SMTP error",
    sentByUserId: "user-1",
    sentAt: "2026-07-16T10:01:00Z",
  },
];

describe("ColdMailHistoryTable — AXE", () => {
  it("brakNaruszen_tabelaZWierszami", async () => {
    const { container } = renderWithChakra(
      <ColdMailHistoryTable items={sampleItems} />
    );
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
