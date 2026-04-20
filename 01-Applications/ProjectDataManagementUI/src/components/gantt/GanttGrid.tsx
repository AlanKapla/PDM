import {
  Box,
  Table,
  Thead,
  Tbody,
  Tr,
  Td,
  useColorModeValue,
  Text,
} from "@chakra-ui/react";
import {
  DndContext,
  PointerSensor,
  closestCenter,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core";
import {
  SortableContext,
  arrayMove,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { useGantt } from "./GanttContext";
import GanttStageRow from "./GanttStageRow";
import GanttGridHeader from "./GanttGridHeader";
import GanttDependencyLayer from "./GanttDependencyLayer";
import type { DateGroup, TimeScale } from "../../hooks/useTimelineData";

const TREE_COLUMN_WIDTH = 300;
const ROW_HEIGHT = 36;

interface GanttGridProps {
  dates: Date[];
  dateGroups: DateGroup[];
  timeScale: TimeScale;
  columnWidth: number;
  hideWeekends: boolean;
  scrollContainerRef: React.RefObject<HTMLDivElement>;
  todayColumnRef: React.RefObject<HTMLTableCellElement>;
}


export default function GanttGrid({
  dates, dateGroups, timeScale, columnWidth, hideWeekends,
  scrollContainerRef, todayColumnRef,
}: GanttGridProps) {
  const { schedule, reorderStages, showDependencies } = useGantt();

  const borderColor = useColorModeValue("gray.200", "gray.700");
  const theadBg = useColorModeValue("gray.50", "gray.700");

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 8 } })
  );

  const stages = schedule?.stages ?? [];
  const stageIds = stages.map(s => s.id);

  const handleStageDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    if (!over || active.id === over.id) return;
    const oldIndex = stageIds.indexOf(String(active.id));
    const newIndex = stageIds.indexOf(String(over.id));
    if (oldIndex < 0 || newIndex < 0) return;
    reorderStages(arrayMove(stageIds, oldIndex, newIndex));
  };

  const isToday = (date: Date) => {
    const today = new Date();
    return date.getDate() === today.getDate() &&
      date.getMonth() === today.getMonth() &&
      date.getFullYear() === today.getFullYear();
  };

  const isWeekend = (date: Date) => {
    const day = date.getDay();
    return day === 0 || day === 6;
  };

  return (
    <Box
      ref={scrollContainerRef}
      overflowX="auto"
      overflowY="auto"
      className="gantt-scroll-container"
    >
      <Box
        position="relative"
        minWidth={`${TREE_COLUMN_WIDTH + dates.length * columnWidth}px`}
      >
        <Table
          variant="unstyled"
          size="sm"
          sx={{
            borderCollapse: "collapse",
            tableLayout: "fixed",
            width: `${TREE_COLUMN_WIDTH + dates.length * columnWidth}px`,
            "& th, & td": {
              borderWidth: "1px",
              borderColor,
              borderStyle: "solid",
              padding: 0,
            },
          }}
        >
          <Thead bg={theadBg} position="sticky" top={0} zIndex={4}>
            <GanttGridHeader
              dateGroups={dateGroups}
              dates={dates}
              timeScale={timeScale}
              columnWidth={columnWidth}
              treeColumnWidth={TREE_COLUMN_WIDTH}
              todayColumnRef={todayColumnRef}
              isToday={isToday}
              isWeekend={isWeekend}
              hideWeekends={hideWeekends}
            />
          </Thead>

          <Tbody>
            {stages.length === 0 && (
              <Tr>
                <Td
                  position="sticky"
                  left={0}
                  bg={theadBg}
                  zIndex={2}
                  width={`${TREE_COLUMN_WIDTH}px`}
                >
                  <Text fontSize="sm" color="gray.500" fontStyle="italic" p={4}>
                    Brak etapów w tym harmonogramie
                  </Text>
                </Td>
                {dates.map((_, idx) => (
                  <Td key={idx} width={`${columnWidth}px`} />
                ))}
              </Tr>
            )}

            <DndContext
              sensors={sensors}
              collisionDetection={closestCenter}
              onDragEnd={handleStageDragEnd}
            >
              <SortableContext items={stageIds} strategy={verticalListSortingStrategy}>
                {stages.map(stage => (
                  <GanttStageRow
                    key={stage.id}
                    stage={stage}
                    depth={0}
                    dates={dates}
                    columnWidth={columnWidth}
                    rowHeight={ROW_HEIGHT}
                    treeColumnWidth={TREE_COLUMN_WIDTH}
                  />
                ))}
              </SortableContext>
            </DndContext>
          </Tbody>
        </Table>

        {/* Overlay SVG zależności */}
        {showDependencies && (
          <GanttDependencyLayer
            dates={dates}
            columnWidth={columnWidth}
            treeColumnWidth={TREE_COLUMN_WIDTH}
            rowHeight={ROW_HEIGHT}
          />
        )}
      </Box>
    </Box>
  );
}
