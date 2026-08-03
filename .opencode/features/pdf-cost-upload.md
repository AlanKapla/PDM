# Feature: Obsługa PDF w module dodawania kosztów/wydatków

## Typ zmiany

**Full-stack** — API (Docnet.Core, ImageSharp, walidacja, AI multi-image) + UI (dropzone, komunikaty, podgląd PDF)

## Cel

Rozszerzenie uploadu kosztów/wydatków (ścieżka manualna i AI) o pliki PDF obok JPG/JPEG/PNG.
AI operuje wyłącznie na obrazach — PDF jest konwertowany in-memory do JPG (wszystkie strony, max 20).
W blob storage zawsze trafia **oryginał** (PDF pozostaje PDF).

## Powiązane feature specs

- [ai-cost-document-import.md](./ai-cost-document-import.md) — single-file AI (planował Docnet strona 1; niezaimplementowane)
- [multi-file-ai-cost-upload.md](./multi-file-ai-cost-upload.md) — batch AI (otwarte pytanie o PDF — ten feature zamyka)

## Zatwierdzone decyzje techniczne

| Decyzja | Wartość |
|--------|---------|
| DPI renderowania | 175 |
| Biblioteki | Docnet.Core + SixLabors.ImageSharp |
| PNG | zostawić (regresja = 0) |
| Limit ProjectCost document | 50 MB (wyrównanie z Kestrel/nginx) |
| Soft cap stron PDF | 20 |
| Soft-fail batch | `rejectedFiles[]` w response + per-item w workerze |

---

## Zakres

### API
- NuGet: Docnet.Core, SixLabors.ImageSharp
- `IPdfToImageConverter` — PDF → uporządkowana kolekcja JPG (in-memory)
- Rozszerzenie Vision: `CompleteWithImagesAsync` (wiele stron)
- `DocumentParserService` — PDF→obrazy→AI; JPG/PNG bez zmian
- Walidacja: rozszerzenie + MIME + magic bytes
- Soft-fail w `SubmitAICostImportBatch`
- Worker: konwersja przed parse; password/corrupt → ErrorNeedsReview z komunikatem PL
- Blob: zawsze oryginał; brak artefaktów JPG
- `DocumentValidationHelper`: MaxDocumentSize → 50 MB

### UI
- `MultiDocumentDropzone` / `DocumentDropzone`: `.jpg,.jpeg,.png,.pdf`
- Soft-fail komunikaty; mixed batch
- `AICostImportModal` — zaktualizowany copy
- `AICostReviewItem` — podgląd PDF (iframe / nowa karta przez SAS)
- `CostForm` — `accept` na input

### Poza zakresem
- Zapis skonwertowanych JPG do blob
- Zmiany schematu DB / migracje
- Zmiana limitu single-parse 20 MB (bez zmian względem multi-file spec)

---

## Przepływ danych

```
Upload (JPG | PDF | mixed batch)
        │
        ├─ Walidacja: extension + MIME + magic bytes
        │     └─ fail → rejectedFiles[] / ErrorNeedsReview (reszta OK)
        │
        ├─ Blob: oryginał (PDF lub JPG)
        │
        └─ Ścieżka AI:
              PDF → Docnet (175 DPI, max 20 stron) → ImageSharp JPG[]
                    → CompleteWithImagesAsync → ParsedCostDto
              JPG/PNG → CompleteWithImage(s)Async bezpośrednio
```

Manual (bez AI): PDF → blob, bez konwersji.

---

## Przypadki brzegowe

| Case | Zachowanie |
|------|------------|
| PDF wielostronicowy | Wszystkie strony (≤20) → AI, kolejność zachowana |
| PDF > 20 stron | Soft cap — błąd czytelny / tylko pierwsze 20 (decydowane w implementacji: odrzuć z komunikatem limitu stron) |
| PDF z hasłem | Odrzuć: „Plik PDF jest zabezpieczony hasłem i nie może zostać przetworzony” |
| PDF uszkodzony | Odrzuć: „Nie udało się odczytać pliku PDF – plik może być uszkodzony” |
| Limit rozmiaru | 50 MB per file (ProjectCost) / łączny batch 50 MB (bez zmian) |
| Podgląd | SAS URL do oryginału |

---

## Kryteria akceptacji

- [ ] Mixed JPG+PDF w jednym uploadzie
- [ ] AI dostaje zestaw JPG (1 na stronę), nie binarkę PDF
- [ ] Blob = oryginał; SAS działa
- [ ] PDF z hasłem → komunikat, reszta paczki OK
- [ ] PDF uszkodzony → komunikat, reszta paczki OK
- [ ] Limit 50 MB JPG i PDF
- [ ] Wielostronicowy PDF — rozpoznanie na próbce
- [ ] Regresja JPG/PNG = 0

---

## Skills do przeczytania przed implementacją

- `.opencode/skills/api-services/SKILL.md`
- `.opencode/skills/api-cqrs/SKILL.md`
- `.opencode/skills/api-validators/SKILL.md`
- `.opencode/skills/api-controllers/SKILL.md`
- `.opencode/skills/api-unit-tests/SKILL.md`
- `.opencode/skills/ui-components/SKILL.md`
- `.opencode/skills/ui-hooks/SKILL.md`
- `.opencode/skills/ui-unit-tests/SKILL.md`
