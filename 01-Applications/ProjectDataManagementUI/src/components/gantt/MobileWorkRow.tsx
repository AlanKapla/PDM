import {
  Box,
  HStack,
  Text,
  Checkbox,
  Badge,
  Avatar,
  AvatarGroup,
  IconButton,
  Menu,
  MenuButton,
  MenuList,
  MenuItem,
  useColorModeValue,
  Spinner,
} from "@chakra-ui/react";
import { MoreVertical, MessageSquare, Calendar, Users, Move, Trash2, GitBranch, Pencil } from "lucide-react";
import { useGantt } from "./GanttContext";
import { fmtCompactDate } from "./ganttRowUtils";
import type { WorkScheduleStageWorkWeb } from "../../types/workSchedule.types";

interface MobileWorkRowProps {
  work: WorkScheduleStageWorkWeb;
  stageId: string;
  depth: number;
}

function computeClosedState(work: WorkScheduleStageWorkWeb): "checked" | "unchecked" | "indeterminate" {
  const periods = work.periods ?? [];
  if (periods.length === 0) return work.isClosed ? "checked" : "unchecked";
  const closedCount = periods.filter(p => p.isClosed).length;
  if (closedCount === periods.length) return "checked";
  if (closedCount === 0) return "unchecked";
  return "indeterminate";
}

function getWorkDates(work: WorkScheduleStageWorkWeb): { start?: string; end?: string } {
  const periods = work.periods ?? [];
  if (periods.length === 0) return {};
  const starts = periods.map(p => p.startDate.slice(0, 10)).sort();
  const ends = periods.map(p => p.endDate.slice(0, 10)).sort();
  return { start: starts[0], end: ends[ends.length - 1] };
}

export default function MobileWorkRow({ work, stageId, depth }: MobileWorkRowProps) {
  const { mode, setWorkIsClosed, isMutating, openMobileModal, canEdit, showComments, deleteWork } = useGantt();

  const isEditing = mode === "edit";
  const isTogglingClosed = isMutating.has(`setWorkIsClosed-${work.id}`);
  const isDeleting = isMutating.has(`deleteWork-${work.id}`);
  const closedState = computeClosedState(work);
  const { start, end } = getWorkDates(work);
  const commentCount = (work.comments ?? []).length;

  const borderColor = useColorModeValue("gray.100", "gray.700");
  const completedBg = useColorModeValue("green.50", "green.900");

  const handleToggleClosed = async () => {
    const newClosed = closedState !== "checked";
    await setWorkIsClosed(stageId, work.id, newClosed);
  };

  const handleDeleteWork = async () => {
    await deleteWork(stageId, work.id);
  };

  return (
    <Box
      borderBottomWidth="1px"
      borderBottomColor={borderColor}
      bg={work.isClosed ? completedBg : "transparent"}
      ml={`${depth * 12}px`}
      px={3}
      py={2}
    >
      <HStack spacing={2} align="start">
        {/* Checkbox IsClosed */}
        <Checkbox
          isChecked={closedState === "checked"}
          isIndeterminate={closedState === "indeterminate"}
          colorScheme="green"
          size="sm"
          isDisabled={isTogglingClosed}
          onChange={handleToggleClosed}
          mt="2px"
          flexShrink={0}
        />

        {/* Kolor indicator */}
        <Box
          w="10px"
          h="10px"
          borderRadius="full"
          bg={work.colorRgb}
          mt="4px"
          flexShrink={0}
        />

        {/* Nazwa + daty + lista */}
        <Box flex={1} minW={0}>
          <Text
            fontSize="sm"
            fontWeight="medium"
            textDecoration={work.isClosed ? "line-through" : "none"}
            color={work.isClosed ? "gray.500" : "inherit"}
            noOfLines={2}
          >
            {work.name || <Text as="span" color="gray.400" fontStyle="italic">Bez nazwy</Text>}
          </Text>

          <HStack spacing={2} mt={1} flexWrap="wrap">
            {start && end && (
              <HStack spacing={1}>
                <Calendar size={10} color="gray" />
                <Text fontSize="xs" color="gray.500">
                  {fmtCompactDate(start)}–{fmtCompactDate(end)}
                </Text>
              </HStack>
            )}

            {(work.assignees ?? []).length > 0 && (
              <AvatarGroup size="2xs" max={3}>
                {work.assignees.map(a => (
                  <Avatar key={a.userId} name={a.userName} size="2xs" />
                ))}
              </AvatarGroup>
            )}

            {showComments && commentCount > 0 && (
              <HStack spacing={1}>
                <MessageSquare size={10} color="gray" />
                <Text fontSize="xs" color="gray.500">{commentCount}</Text>
              </HStack>
            )}
          </HStack>
        </Box>

        {/* Menu akcji */}
        <Menu>
          <MenuButton
            as={IconButton}
            aria-label="Więcej"
            icon={<MoreVertical size={14} />}
            size="xs"
            variant="ghost"
            flexShrink={0}
          />
          <MenuList>
            <MenuItem
              icon={<Pencil size={14} />}
              onClick={() => openMobileModal({ type: "editWork", stageId, work })}
            >
              Edytuj (nazwa / kolor)
            </MenuItem>
            <MenuItem
              icon={<Calendar size={14} />}
              onClick={() => openMobileModal({ type: "periods", stageId, work })}
            >
              Okresy
            </MenuItem>
            <MenuItem
              icon={<Users size={14} />}
              onClick={() => openMobileModal({ type: "assignments", stageId, work })}
            >
              Przypisani
            </MenuItem>
            <MenuItem
              icon={<MessageSquare size={14} />}
              onClick={() => openMobileModal({ type: "comments", stageId, work })}
            >
              Komentarze ({commentCount})
            </MenuItem>
            <MenuItem
              icon={<GitBranch size={14} />}
              onClick={() => openMobileModal({ type: "dependencies" })}
            >
              Zależności
            </MenuItem>
            {isEditing && canEdit && (
              <>
                <MenuItem
                  icon={<Move size={14} />}
                  onClick={() => openMobileModal({ type: "moveWork", stageId, workId: work.id })}
                >
                  Przenieś
                </MenuItem>
                <MenuItem
                  icon={isDeleting ? <Spinner size="xs" /> : <Trash2 size={14} />}
                  color="red.500"
                  isDisabled={isDeleting}
                  onClick={handleDeleteWork}
                >
                  Usuń
                </MenuItem>
              </>
            )}
          </MenuList>
        </Menu>
      </HStack>
    </Box>
  );
}
