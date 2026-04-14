import { Box, Input, Tooltip } from "@chakra-ui/react";
import type { InputProps } from "@chakra-ui/react";
import type { DateConstraints } from "../utils/workScheduleDateConstraints";

interface ConstrainedDateInputProps extends Omit<InputProps, 'type' | 'onChange'> {
  value: string;
  onChange: (value: string) => void;
  /** 'start' — pole daty rozpoczęcia, 'end' — pole daty zakończenia. */
  fieldRole: 'start' | 'end';
  /** Ograniczenia wynikające z zależności między zakresami prac. */
  constraints?: DateConstraints;
  /**
   * Wartość drugiego pola w danym okresie — używana jako wewnętrzna granica
   * (startDate nie może być > endDate i odwrotnie, niezależnie od zewnętrznych constraints).
   */
  otherBound?: string;
}

/**
 * Nakładka na Chakra `Input type="date"` z obsługą ograniczeń dat wynikających z zależności.
 *
 * **Tryb B** – zależności zostały zdefiniowane przed uzupełnieniem dat:
 *   - `min`/`max` na Input blokuje niedostępne daty w natywnym date pickerze
 *   - `isInvalid` (czerwona ramka) gdy bieżąca wartość narusza ograniczenie
 *   - Tooltip z czytelnym wyjaśnieniem dlaczego data jest niedostępna
 *
 * Komponent jest bezstanowy — całość logiki pochodzi z `constraints`.
 */
export function ConstrainedDateInput({
  value,
  onChange,
  fieldRole,
  constraints,
  otherBound,
  ...inputProps
}: ConstrainedDateInputProps) {
  // Oblicz min/max dla pola z uwzględnieniem zarówno ograniczeń zewnętrznych (zależności)
  // jak i wewnętrznych (start ≤ end w ramach tego samego okresu).
  let minDate: string | undefined;
  let maxDate: string | undefined;
  let tooltipReason: string | undefined;

  if (fieldRole === 'start') {
    // min = max(minStartDate z dep, -)
    minDate = constraints?.minStartDate;
    tooltipReason = constraints?.minStartDateReason;
    // max = min(maxStartDate z dep, endDate okresu)
    const candidates = [constraints?.maxStartDate, otherBound].filter(Boolean) as string[];
    maxDate = candidates.length > 0 ? candidates.sort()[0] : undefined;
    if (!tooltipReason && constraints?.maxStartDateReason) tooltipReason = constraints.maxStartDateReason;
  } else {
    // min = max(minEndDate z dep, startDate okresu)
    const candidates = [constraints?.minEndDate, otherBound].filter(Boolean) as string[];
    minDate = candidates.length > 0 ? candidates.sort().reverse()[0] : undefined;
    tooltipReason = constraints?.minEndDateReason;
    // max = maxEndDate z dep
    maxDate = constraints?.maxEndDate;
    if (!tooltipReason && constraints?.maxEndDateReason) tooltipReason = constraints.maxEndDateReason;
  }

  const isViolated = Boolean(
    (minDate && value && value < minDate) ||
    (maxDate && value && value > maxDate)
  );

  const input = (
    <Input
      type="date"
      value={value}
      onChange={(e) => onChange(e.target.value)}
      min={minDate}
      max={maxDate}
      isInvalid={isViolated}
      {...inputProps}
    />
  );

  // Tooltip pokazywany gdy:
  // - wartość narusza ograniczenie (ActiveViolation), LUB
  // - ograniczenie aktywne (żeby user zawsze wiedział dlaczego zakres jest blokowany)
  const showTooltip = tooltipReason && (isViolated || minDate || maxDate);

  if (!showTooltip) return input;

  return (
    <Tooltip
      label={tooltipReason}
      placement="top"
      hasArrow
      // Wymuszamy wyświetlenie tooltipa gdy wartość narusza ograniczenie;
      // w przeciwnym razie tooltip jest widoczny tylko na hover.
      isOpen={isViolated ? true : undefined}
    >
      {/* Box przejmuje flex/minW props z zewnątrz bo Tooltip nie przekazuje ich dalej */}
      <Box
        flex={inputProps.flex}
        minW={inputProps.minW}
        w={inputProps.w}
        display="inline-block"
      >
        {input}
      </Box>
    </Tooltip>
  );
}
