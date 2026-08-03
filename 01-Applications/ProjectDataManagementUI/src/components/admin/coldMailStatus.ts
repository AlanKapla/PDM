const STATUS_LABELS: Record<string, string> = {
  Queued: "W kolejce",
  Sent: "Wysłany",
  Failed: "Błąd",
};

const STATUS_COLOR_SCHEMES: Record<string, string> = {
  Queued: "blue",
  Sent: "green",
  Failed: "red",
};

export function formatColdMailStatus(status: string): string {
  return STATUS_LABELS[status] ?? status;
}

export function coldMailStatusColorScheme(status: string): string {
  return STATUS_COLOR_SCHEMES[status] ?? "gray";
}
