import {
  Box,
  Button,
  HStack,
  Slider,
  SliderFilledTrack,
  SliderThumb,
  SliderTrack,
  Text,
  VStack,
} from "@chakra-ui/react";
import { CalendarDays, Eye, EyeOff } from "lucide-react";
import type { TimeScale } from "../hooks/useTimelineData";

interface TimelineToolbarProps {
  /** Aktywna skala czasu */
  timeScale: TimeScale;
  setTimeScale: (scale: TimeScale) => void;
  /** Zakres ± miesiące */
  timeRangeMonths: number;
  setTimeRangeMonths: (v: number) => void;
  /** Ukrywanie weekendów */
  hideWeekends: boolean;
  toggleWeekends: () => void;
  /** Scroll do dzisiejszej daty */
  scrollToToday: () => void;
  /** Rozwiń / Zwiń wszystko */
  onExpandAll?: () => void;
  onCollapseAll?: () => void;
  /** Responsywność */
  isMobile?: boolean;
}

/**
 * Wspólny pasek kontrolek timeline'u.
 * Używany w AssignedWorks i WorkScheduleView.
 *
 * Layout:
 *   Wiersz 1: [Skala: Dni|Tygodnie|Miesiące]  [Ukryj weekendy]  [Dzisiaj]  [Rozwiń | Zwiń]
 *   Wiersz 2: [Zakres: ─────●───── ±N miesięcy]
 */
export default function TimelineToolbar({
  timeScale,
  setTimeScale,
  timeRangeMonths,
  setTimeRangeMonths,
  hideWeekends,
  toggleWeekends,
  scrollToToday,
  onExpandAll,
  onCollapseAll,
  isMobile = false,
}: TimelineToolbarProps) {
  const btnSize = isMobile ? "xs" : "sm";
  const fontSize = isMobile ? "10px" : "sm";
  const labelFontSize = isMobile ? "10px" : "sm";

  return (
    <VStack spacing={2} align="stretch" w="100%">
      {/* ── Wiersz 1: kontrolki ── */}
      <HStack spacing={2} flexWrap="wrap" gap={2} justify="space-between">
        {/* Lewa strona: skala czasu */}
        <HStack spacing={2} flexWrap="wrap">
          <Text fontWeight="medium" fontSize={labelFontSize} whiteSpace="nowrap">
            Skala:
          </Text>
          {(["days", "weeks", "months"] as TimeScale[]).map((scale) => (
            <Button
              key={scale}
              size={btnSize}
              variant={timeScale === scale ? "solid" : "outline"}
              colorScheme="level2"
              onClick={() => setTimeScale(scale)}
              fontSize={fontSize}
            >
              {scale === "days" ? "Dni" : scale === "weeks" ? "Tygodnie" : "Miesiące"}
            </Button>
          ))}
        </HStack>

        {/* Prawa strona: przełączniki */}
        <HStack spacing={2} flexWrap="wrap">
          <Button
            size={btnSize}
            variant={hideWeekends ? "solid" : "outline"}
            colorScheme="gray"
            onClick={toggleWeekends}
            fontSize={fontSize}
            leftIcon={hideWeekends ? <Eye size={14} /> : <EyeOff size={14} />}
          >
            {hideWeekends ? "Pokaż weekendy" : "Ukryj weekendy"}
          </Button>

          <Button
            size={btnSize}
            leftIcon={<CalendarDays size={14} />}
            colorScheme="primary"
            onClick={scrollToToday}
            fontSize={fontSize}
          >
            Dzisiaj
          </Button>

          {onExpandAll && onCollapseAll && (
            <>
              <Button
                size={btnSize}
                variant="outline"
                onClick={onExpandAll}
                fontSize={fontSize}
              >
                Rozwiń
              </Button>
              <Button
                size={btnSize}
                variant="outline"
                onClick={onCollapseAll}
                fontSize={fontSize}
              >
                Zwiń
              </Button>
            </>
          )}
        </HStack>
      </HStack>

      {/* ── Wiersz 2: zakres czasu ── */}
      <HStack spacing={2}>
        <Text fontWeight="medium" fontSize={labelFontSize} whiteSpace="nowrap">
          Zakres:
        </Text>
        <Slider
          value={timeRangeMonths}
          onChange={setTimeRangeMonths}
          min={1}
          max={24}
          step={1}
          colorScheme="level2"
          flex={1}
          maxW={isMobile ? "200px" : "400px"}
        >
          <SliderTrack>
            <SliderFilledTrack />
          </SliderTrack>
          <SliderThumb boxSize={6}>
            <Box color="level2.500" fontSize="2xs" fontWeight="bold">
              {timeRangeMonths}
            </Box>
          </SliderThumb>
        </Slider>
        <Text fontSize="xs" color="gray.600" whiteSpace="nowrap">
          ±{timeRangeMonths}{" "}
          {timeRangeMonths === 1
            ? "miesiąc"
            : timeRangeMonths < 5
              ? "miesiące"
              : "miesięcy"}
        </Text>
      </HStack>
    </VStack>
  );
}
