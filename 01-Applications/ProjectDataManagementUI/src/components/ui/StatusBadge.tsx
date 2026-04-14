import { Badge } from "@chakra-ui/react";

type StatusVariant =
  | "active"
  | "inactive"
  | "pending"
  | "completed"
  | "resolved"
  | "unresolved"
  | "approved"
  | "rejected"
  | "draft"
  | "cancelled"
  | string;

const STATUS_MAP: Record<string, { label: string; colorScheme: string }> = {
  active:     { label: "Aktywny",       colorScheme: "green"  },
  inactive:   { label: "Nieaktywny",    colorScheme: "gray"   },
  pending:    { label: "Oczekuje",      colorScheme: "yellow" },
  completed:  { label: "Ukończony",     colorScheme: "blue"   },
  resolved:   { label: "Rozwiązany",    colorScheme: "green"  },
  unresolved: { label: "Nierozwiązany", colorScheme: "orange" },
  approved:   { label: "Zatwierdzone",  colorScheme: "green"  },
  rejected:   { label: "Odrzucone",     colorScheme: "red"    },
  draft:      { label: "Szkic",         colorScheme: "gray"   },
  cancelled:  { label: "Anulowane",     colorScheme: "red"    },
};

interface StatusBadgeProps {
  /** Klucz statusu (np. "active", "pending") lub własna etykieta */
  status: StatusVariant;
  /** Nadpisuje automatycznie dobraną etykietę */
  label?: string;
  /** Nadpisuje automatycznie dobrany schemat kolorów */
  colorScheme?: string;
  size?: string;
}

/**
 * Jednolity badge statusu dla całej aplikacji.
 * Automatycznie dobiera kolor i etykietę na podstawie klucza statusu.
 */
export default function StatusBadge({
  status,
  label,
  colorScheme,
  size,
}: StatusBadgeProps) {
  const config = STATUS_MAP[status.toLowerCase()] ?? {
    label: status,
    colorScheme: "gray",
  };

  return (
    <Badge
      colorScheme={colorScheme ?? config.colorScheme}
      fontSize={size}
      borderRadius="md"
      px={2}
    >
      {label ?? config.label}
    </Badge>
  );
}
