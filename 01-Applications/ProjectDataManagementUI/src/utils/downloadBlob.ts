/**
 * Parsuje nazwę pliku z nagłówka Content-Disposition.
 * Obsługuje `filename="..."` oraz `filename*=UTF-8''...`.
 */
export function parseContentDispositionFileName(
  header: string | undefined
): string | null {
  if (!header || header.trim().length === 0) {
    return null;
  }

  const starMatch: RegExpMatchArray | null = header.match(
    /filename\*\s*=\s*(?:UTF-8''|utf-8'')([^;]+)/i
  );
  if (starMatch?.[1]) {
    const encoded: string = starMatch[1].trim().replace(/^["']|["']$/g, '');
    try {
      return decodeURIComponent(encoded);
    } catch {
      return encoded;
    }
  }

  const plainMatch: RegExpMatchArray | null = header.match(
    /filename\s*=\s*([^;]+)/i
  );
  if (plainMatch?.[1]) {
    return plainMatch[1].trim().replace(/^["']|["']$/g, '');
  }

  return null;
}

/**
 * Wymusza pobranie bloba przez tymczasowy element `<a download>`.
 */
export function downloadBlob(blob: Blob, fileName: string): void {
  const objectUrl: string = URL.createObjectURL(blob);
  const anchor: HTMLAnchorElement = document.createElement('a');
  anchor.href = objectUrl;
  anchor.download = fileName;
  anchor.rel = 'noopener';
  document.body.appendChild(anchor);
  anchor.click();
  document.body.removeChild(anchor);
  URL.revokeObjectURL(objectUrl);
}

/**
 * Sanityzuje nazwę pliku (usuwa znaki niedozwolone w systemach plików).
 * Usuwa też końcówki .pdf/.xlsx/.xls, żeby nie dublować rozszerzenia przy doklejaniu daty.
 */
export function sanitizeDownloadFileName(name: string): string {
  const withoutExtension: string = name.replace(/\.(pdf|xlsx|xls)$/i, '');
  const sanitized: string = withoutExtension
    .replace(/[<>:"/\\|?*\u0000-\u001F]/g, '_')
    .replace(/\s+/g, ' ')
    .trim();
  return sanitized.length > 0 ? sanitized : 'kosztorys';
}
