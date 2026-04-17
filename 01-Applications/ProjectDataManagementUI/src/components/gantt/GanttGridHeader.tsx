import { Tr, Th, Text, Box, useColorModeValue } from "@chakra-ui/react";
import type { DateGroup, TimeScale } from "../../hooks/useTimelineData";

interface GanttGridHeaderProps {
  dateGroups: DateGroup[];
  dates: Date[];
  timeScale: TimeScale;
  columnWidth: number;
  treeColumnWidth: number;
  todayColumnRef: React.RefObject<HTMLTableCellElement>;
  isToday: (d: Date) => boolean;
  isWeekend: (d: Date) => boolean;
  hideWeekends: boolean;
}

function formatDate(date: Date, scale: TimeScale): string {
  if (scale === "months") {
    return `${date.getDate()}`;
  }
  if (scale === "weeks") {
    const days = ["Nd", "Pn", "Wt", "Śr", "Cz", "Pt", "So"];
    return `${days[date.getDay()]}\n${date.getDate()}.${date.getMonth() + 1}`;
  }
  return `${date.getDate()}.${date.getMonth() + 1}`;
}

export default function GanttGridHeader({
  dateGroups, dates, timeScale, columnWidth, treeColumnWidth,
  todayColumnRef, isToday, isWeekend, hideWeekends,
}: GanttGridHeaderProps) {
  const theadBg = useColorModeValue("gray.50", "gray.700");
  const todayBg = useColorModeValue("primary.100", "primary.800");
  const todayColor = useColorModeValue("primary.700", "primary.200");
  const weekendBg = useColorModeValue("gray.100", "gray.700");
  const borderColor = useColorModeValue("gray.200", "gray.600");

  return (
    <>
      {/* Wiersz 1: grupy (tygodnie/miesiące) */}
      <Tr>
        <Th
          position="sticky"
          left={0}
          bg={theadBg}
          zIndex={3}
          width={`${treeColumnWidth}px`}
          minWidth={`${treeColumnWidth}px`}
          maxWidth={`${treeColumnWidth}px`}
        />
        {dateGroups.map((group, idx) => (
          <Th
            key={idx}
            colSpan={group.count}
            bg={theadBg}
            textAlign="center"
            fontSize="11px"
            fontWeight="semibold"
            borderRightWidth="2px"
            borderRightColor={borderColor}
            px={1}
            py={1}
          >
            <Text noOfLines={1}>{group.label}</Text>
          </Th>
        ))}
      </Tr>

      {/* Wiersz 2: poszczególne dni */}
      <Tr>
        <Th
          position="sticky"
          left={0}
          bg={theadBg}
          zIndex={3}
          width={`${treeColumnWidth}px`}
          minWidth={`${treeColumnWidth}px`}
          maxWidth={`${treeColumnWidth}px`}
        >
          <Text fontSize="11px" fontWeight="semibold" px={2}>Etap / Zakres pracy</Text>
        </Th>
        {dates.map((date, idx) => {
          const today = isToday(date);
          const weekend = isWeekend(date);
          const label = formatDate(date, timeScale);

          return (
            <Th
              key={idx}
              ref={today ? todayColumnRef : undefined}
              width={`${columnWidth}px`}
              minWidth={`${columnWidth}px`}
              maxWidth={`${columnWidth}px`}
              bg={today ? todayBg : weekend ? weekendBg : theadBg}
              borderRightWidth="1px"
              borderRightColor={borderColor}
              textAlign="center"
              px={0}
              py={1}
              position="relative"
            >
              <Text
                fontSize="9px"
                fontWeight={today ? "bold" : "normal"}
                color={today ? todayColor : "inherit"}
                whiteSpace="pre"
                lineHeight="tight"
              >
                {label}
              </Text>
              {today && (
                <Box
                  position="absolute"
                  bottom={0}
                  left="50%"
                  transform="translateX(-50%)"
                  w="2px"
                  h="4px"
                  bg={todayColor}
                />
              )}
            </Th>
          );
        })}
      </Tr>
    </>
  );
}
