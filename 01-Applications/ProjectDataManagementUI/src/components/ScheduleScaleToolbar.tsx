import React, { useEffect, useRef, useState } from "react";
import {
  Box,
  Button,
  HStack,
  IconButton,
  Menu,
  MenuButton,
  MenuItem,
  MenuList,
  Slider,
  SliderFilledTrack,
  SliderThumb,
  SliderTrack,
  Text,
  Tooltip,
  VStack,
} from "@chakra-ui/react";
import { ChevronDown, Minus, Plus } from "lucide-react";
import type { TimeScale } from "../hooks/useTimelineData";

// ─── Types ────────────────────────────────────────────────────────────────────

interface ScheduleScaleToolbarProps {
  timeScale: TimeScale;
  setTimeScale: (scale: TimeScale) => void;
  timeRangeMonths: number;
  setTimeRangeMonths: (v: number) => void;
}

type ScaleBp = "full" | "compact" | "mobile";

// ─── Helpers ─────────────────────────────────────────────────────────────────

const SCALE_LABELS: Record<TimeScale, string> = {
  days: "Dni",
  weeks: "Tygodnie",
  months: "Miesiące",
};

function rangeLabel(months: number): string {
  if (months === 1) return "miesiąc";
  if (months < 5) return "miesiące";
  return "miesięcy";
}

// ─── Component ────────────────────────────────────────────────────────────────

export default function ScheduleScaleToolbar({
  timeScale,
  setTimeScale,
  timeRangeMonths,
  setTimeRangeMonths,
}: ScheduleScaleToolbarProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [bp, setBp] = useState<ScaleBp>("full");

  useEffect(() => {
    const el = containerRef.current;
    if (!el) return;

    const measure = (w: number) =>
      setBp(w >= 640 ? "full" : w >= 380 ? "compact" : "mobile");

    measure(el.offsetWidth);

    const obs = new ResizeObserver(([entry]) =>
      measure(entry.contentRect.width)
    );
    obs.observe(el);
    return () => obs.disconnect();
  }, []);

  const adjustRange = (delta: number) =>
    setTimeRangeMonths(Math.min(24, Math.max(1, timeRangeMonths + delta)));

  // ─── Shared: zakres (±) przyciskami — używany w compact i mobile ──────────

  const RangeSteppers = ({ size = "sm" }: { size?: string }) => (
    <HStack spacing={1}>
      <Tooltip label="Zmniejsz zakres" hasArrow placement="top">
        <IconButton
          aria-label="Zmniejsz zakres"
          icon={<Minus size={12} />}
          size={size as any}
          variant="outline"
          colorScheme="level2"
          isDisabled={timeRangeMonths <= 1}
          onClick={() => adjustRange(-1)}
        />
      </Tooltip>
      <Text
        fontWeight="semibold"
        fontSize="sm"
        color="level2.600"
        minW="60px"
        textAlign="center"
        userSelect="none"
      >
        ±{timeRangeMonths} {rangeLabel(timeRangeMonths)}
      </Text>
      <Tooltip label="Zwiększ zakres" hasArrow placement="top">
        <IconButton
          aria-label="Zwiększ zakres"
          icon={<Plus size={12} />}
          size={size as any}
          variant="outline"
          colorScheme="level2"
          isDisabled={timeRangeMonths >= 24}
          onClick={() => adjustRange(1)}
        />
      </Tooltip>
    </HStack>
  );

  // ─── Shared: skala jako Menu dropdown ────────────────────────────────────

  const ScaleMenu = ({ size = "sm" }: { size?: string }) => (
    <Menu>
      <MenuButton
        as={Button}
        rightIcon={<ChevronDown size={12} />}
        size={size as any}
        colorScheme="level2"
        variant="outline"
      >
        Skala: {SCALE_LABELS[timeScale]}
      </MenuButton>
      <MenuList minW="160px">
        {(["days", "weeks", "months"] as TimeScale[]).map((scale) => (
          <MenuItem
            key={scale}
            onClick={() => setTimeScale(scale)}
            fontWeight={timeScale === scale ? "semibold" : "normal"}
            color={timeScale === scale ? "level2.600" : undefined}
          >
            {SCALE_LABELS[scale]}
          </MenuItem>
        ))}
      </MenuList>
    </Menu>
  );

  // ─── Layouts ──────────────────────────────────────────────────────────────

  return (
    <Box ref={containerRef} w="100%">
      {/* ── FULL (≥640px): skala inline + slider ── */}
      {bp === "full" && (
        <HStack spacing={4} align="center" flexWrap="wrap">
          {/* Skala */}
          <HStack spacing={2}>
            <Text
              fontWeight="medium"
              fontSize="sm"
              whiteSpace="nowrap"
              color="gray.600"
            >
              Skala:
            </Text>
            {(["days", "weeks", "months"] as TimeScale[]).map((scale) => (
              <Button
                key={scale}
                size="sm"
                variant={timeScale === scale ? "solid" : "outline"}
                colorScheme="level2"
                onClick={() => setTimeScale(scale)}
              >
                {SCALE_LABELS[scale]}
              </Button>
            ))}
          </HStack>

          {/* Zakres */}
          <HStack spacing={2} flex={1} minW="180px" maxW="480px">
            <Text
              fontWeight="medium"
              fontSize="sm"
              whiteSpace="nowrap"
              color="gray.600"
            >
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
            <Text fontSize="xs" color="gray.500" whiteSpace="nowrap">
              ±{timeRangeMonths} {rangeLabel(timeRangeMonths)}
            </Text>
          </HStack>
        </HStack>
      )}

      {/* ── COMPACT (380–639px): skala inline (mniejsze), zakres — steppers ── */}
      {bp === "compact" && (
        <VStack spacing={2} align="stretch">
          <HStack spacing={2} justify="space-between" flexWrap="wrap">
            <HStack spacing={1}>
              <Text
                fontWeight="medium"
                fontSize="xs"
                whiteSpace="nowrap"
                color="gray.600"
              >
                Skala:
              </Text>
              {(["days", "weeks", "months"] as TimeScale[]).map((scale) => (
                <Button
                  key={scale}
                  size="xs"
                  variant={timeScale === scale ? "solid" : "outline"}
                  colorScheme="level2"
                  onClick={() => setTimeScale(scale)}
                >
                  {SCALE_LABELS[scale]}
                </Button>
              ))}
            </HStack>
            <RangeSteppers size="xs" />
          </HStack>
        </VStack>
      )}

      {/* ── MOBILE (<380px): dropdown + steppers ── */}
      {bp === "mobile" && (
        <HStack spacing={2} justify="space-between" flexWrap="wrap">
          <ScaleMenu size="xs" />
          <RangeSteppers size="xs" />
        </HStack>
      )}
    </Box>
  );
}
