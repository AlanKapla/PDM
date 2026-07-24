import { useState, useRef, useEffect } from "react";
import {
  Box,
  Text,
  HStack,
  IconButton,
  Badge,
  Input,
  Tooltip,
  AlertDialog,
  AlertDialogBody,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogContent,
  AlertDialogOverlay,
  Button,
  useDisclosure,
  useColorModeValue,
  Avatar,
  AvatarGroup,
  Popover,
  PopoverTrigger,
  PopoverContent,
  PopoverBody,
  PopoverArrow,
  Checkbox,
  VStack,
  Spinner,
  Tr,
  Td,
} from "@chakra-ui/react";
import { Trash2, GripVertical, MessageSquare, UserPlus } from "lucide-react";
import { useSortable } from "@dnd-kit/sortable";
import { useGantt } from "./GanttContext";
import GanttBar from "./GanttBar";
import { GanttTruncatedName } from "./GanttTruncatedName";
import { makeDateColMap } from "./ganttRowUtils";
import GanttCommentPopover from "./GanttCommentPopover";
import type { WorkScheduleStageWorkWeb } from "../../types/workSchedule.types";

// ─── Stałe ────────────────────────────────────────────────────────────────────
const DEPTH_INDENT = 20;

/** Zwraca indeks kolumny (0-based) odpowiadający dacie z ciągu dat */
function findColIdx(dates: Date[], dateStr: string): number {
  const target = dateStr.slice(0, 10);
  return dates.findIndex(d => {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, "0");
    const day = String(d.getDate()).padStart(2, "0");
    return `${y}-${m}-${day}` === target;
  });
}

interface GanttWorkRowProps {
  work: WorkScheduleStageWorkWeb;
  stageId: string;
  depth: number;
  dates: Date[];
  columnWidth: number;
  rowHeight: number;
  treeColumnWidth: number;
}

/**
 * Oblicza stan checkboxa zakresu pracy na podstawie periodów.
 * - wszystkie closed → checked
 * - żaden → unchecked
 * - część → indeterminate
 */
function computeClosedState(work: WorkScheduleStageWorkWeb): "checked" | "unchecked" | "indeterminate" {
  const periods = work.periods ?? [];
  if (periods.length === 0) return work.isClosed ? "checked" : "unchecked";
  const closedCount = periods.filter(p => p.isClosed).length;
  if (closedCount === periods.length) return "checked";
  if (closedCount === 0) return "unchecked";
  return "indeterminate";
}

export default function GanttWorkRow({
  work, stageId, depth, dates, columnWidth, rowHeight, treeColumnWidth,
}: GanttWorkRowProps) {
  const {
    mode, renameWork, deleteWork, setWorkIsClosed, setAssignments, setPeriods, members, contractors, isMutating,
  } = useGantt();

  const [isEditingName, setIsEditingName] = useState(false);
  const [nameInput, setNameInput] = useState(work.name);
  const [isHovered, setIsHovered] = useState(false);
  const [showInlineComments, setShowInlineComments] = useState(false);
  const { isOpen: isDeleteOpen, onOpen: onDeleteOpen, onClose: onDeleteClose } = useDisclosure();
  const cancelRef = useRef<HTMLButtonElement>(null);

  const renameDebounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const assignmentDebounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const isSelectingCells = useRef(false);
  const cellSelectionRef = useRef<{ startIdx: number; endIdx: number } | null>(null);
  const [cellSelection, setCellSelection] = useState<{ startIdx: number; endIdx: number } | null>(null);

  // Lokalne zaznaczenie przypisanych — debounce przed wysłaniem do API
  const [selectedUserIds, setSelectedUserIds] = useState<string[]>(
    () => (work.assignees ?? []).map(a => a.userId).filter((id): id is string => !!id)
  );
  const [selectedContractorIds, setSelectedContractorIds] = useState<string[]>(
    () => (work.assignees ?? []).map(a => a.contractorId).filter((id): id is string => !!id)
  );
  const selectedUserIdsRef = useRef(selectedUserIds);
  const selectedContractorIdsRef = useRef(selectedContractorIds);
  selectedUserIdsRef.current = selectedUserIds;
  selectedContractorIdsRef.current = selectedContractorIds;
  useEffect(() => {
    if (!assignmentDebounceRef.current) {
      setSelectedUserIds(
        (work.assignees ?? []).map(a => a.userId).filter((id): id is string => !!id)
      );
      setSelectedContractorIds(
        (work.assignees ?? []).map(a => a.contractorId).filter((id): id is string => !!id)
      );
    }
  }, [work.id, work.assignees?.length]);

  // Cleanup debounce timers
  useEffect(() => {
    return () => {
      if (renameDebounceRef.current) clearTimeout(renameDebounceRef.current);
      if (assignmentDebounceRef.current) clearTimeout(assignmentDebounceRef.current);
    };
  }, []);

  const isEditing = mode === "edit";
  const isDeleting = isMutating.has(`deleteWork-${work.id}`);
  const isRenaming = isMutating.has(`renameWork-${work.id}`);
  const isTogglingClosed = isMutating.has(`setWorkIsClosed-${work.id}`);
  const closedState = computeClosedState(work);
  const commentCount = (work.comments ?? []).length;

  const borderColor = useColorModeValue("gray.200", "gray.700");
  const gridLineColor = useColorModeValue("#E2E8F0", "#2D3748");
  const hoverBg = useColorModeValue("gray.50", "gray.750");
  const completedBg = useColorModeValue("green.50", "green.900");
  const commentsBg = useColorModeValue("blue.50", "blue.900");
  const selectionBg = useColorModeValue("primary.200", "primary.700");

  // ─── DnD useSortable ────────────────────────────────────────────────────────
  const {
    attributes: sortableAttrs,
    listeners: sortableListeners,
    setNodeRef: setSortableRef,
    setActivatorNodeRef: setSortableActivatorRef,
    transform: sortableTransform,
    transition: sortableTransition,
    isDragging: isSortableDragging,
  } = useSortable({ id: work.id });

  const workSortableStyle: React.CSSProperties = {
    transform: sortableTransform
      ? `translate3d(${sortableTransform.x}px, ${sortableTransform.y}px, 0)`
      : undefined,
    transition: sortableTransition ?? undefined,
    opacity: isSortableDragging ? 0.5 : 1,
  };
  const rowBg = work.isClosed ? completedBg : (isHovered ? hoverBg : "transparent");

  // ─── Rename — klik w trybie edycji ──────────────────────────────────────────
  const handleClick = () => {
    if (!isEditing) return;
    setNameInput(work.name);
    setIsEditingName(true);
  };

  const handleNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    setNameInput(value);
    if (renameDebounceRef.current) clearTimeout(renameDebounceRef.current);
    renameDebounceRef.current = setTimeout(async () => {
      const trimmed = value.trim();
      if (!trimmed || trimmed === work.name) return;
      await renameWork(stageId, work.id, trimmed);
    }, 700);
  };

  const commitName = async () => {
    setIsEditingName(false);
    if (renameDebounceRef.current) {
      clearTimeout(renameDebounceRef.current);
      renameDebounceRef.current = null;
    }
    const trimmed = nameInput.trim();
    if (!trimmed || trimmed === work.name) return;
    await renameWork(stageId, work.id, trimmed);
  };

  const handleNameKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Enter") (e.target as HTMLInputElement).blur();
    if (e.key === "Escape") {
      setIsEditingName(false);
      if (renameDebounceRef.current) clearTimeout(renameDebounceRef.current);
      setNameInput(work.name);
    }
  };

  // ─── IsClosed toggle ─────────────────────────────────────────────────────────
  const handleToggleClosed = async () => {
    const newClosed = closedState !== "checked";
    await setWorkIsClosed(stageId, work.id, newClosed);
  };

  const handleDeleteConfirm = async () => {
    onDeleteClose();
    await deleteWork(stageId, work.id);
  };

  // ─── Assignees z debounce 700ms ──────────────────────────────────────────────
  const scheduleAssignmentSave = (userIds: string[], contractorIds: string[]) => {
    if (assignmentDebounceRef.current) clearTimeout(assignmentDebounceRef.current);
    assignmentDebounceRef.current = setTimeout(() => {
      setAssignments(stageId, work.id, userIds, contractorIds);
    }, 700);
  };

  const handleToggleAssignee = (userId: string) => {
    setSelectedUserIds(prev => {
      const newIds = prev.includes(userId)
        ? prev.filter(id => id !== userId)
        : [...prev, userId];
      scheduleAssignmentSave(newIds, selectedContractorIdsRef.current);
      return newIds;
    });
  };

  const handleToggleContractor = (contractorId: string) => {
    setSelectedContractorIds(prev => {
      const newIds = prev.includes(contractorId)
        ? prev.filter(id => id !== contractorId)
        : [...prev, contractorId];
      scheduleAssignmentSave(selectedUserIdsRef.current, newIds);
      return newIds;
    });
  };

  const getMemberDisplayName = (m: typeof members[0]) => {
    const name = [m.firstName, m.lastName].filter(Boolean).join(" ") || m.email;
    return m.companyName?.trim() ? `${name} (${m.companyName.trim()})` : name;
  };

  // ─── Tworzenie okresu przez zaznaczenie komórek ──────────────────────────────
  const toLocalDateStr = (d: Date) =>
    `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;

  const getHitIdx = (e: React.MouseEvent<HTMLTableCellElement>) => {
    const container = e.currentTarget.closest('.gantt-scroll-container') as HTMLElement | null;
    const scrollLeft = container?.scrollLeft ?? 0;
    const x = scrollLeft + e.clientX - treeColumnWidth;
    return Math.max(0, Math.min(dates.length - 1, Math.floor(x / columnWidth)));
  };

  const handleTimelineMD = (e: React.MouseEvent<HTMLTableCellElement>) => {
    if (!isEditing) return;
    e.preventDefault();
    const idx = getHitIdx(e);
    const sel = { startIdx: idx, endIdx: idx };
    isSelectingCells.current = true;
    cellSelectionRef.current = sel;
    setCellSelection(sel);
  };

  const handleTimelineMM = (e: React.MouseEvent<HTMLTableCellElement>) => {
    if (!isSelectingCells.current || !cellSelectionRef.current) return;
    const idx = getHitIdx(e);
    const newSel = { ...cellSelectionRef.current, endIdx: idx };
    cellSelectionRef.current = newSel;
    setCellSelection(newSel);
  };

  const handleTimelineMU = () => {
    if (!isSelectingCells.current || !cellSelectionRef.current) return;
    isSelectingCells.current = false;
    const { startIdx, endIdx } = cellSelectionRef.current;
    cellSelectionRef.current = null;
    setCellSelection(null);
    const s = Math.min(startIdx, endIdx);
    const eIdx = Math.max(startIdx, endIdx);
    const startDate = toLocalDateStr(dates[s]);
    const endDateStr = toLocalDateStr(dates[eIdx]);
    const existingPeriods = (work.periods ?? []).map(p => ({
      startDate: p.startDate.slice(0, 10),
      endDate: p.endDate.slice(0, 10),
      isClosed: p.isClosed,
    }));
    void setPeriods(stageId, work.id, [
      ...existingPeriods,
      { startDate, endDate: endDateStr, isClosed: false },
    ]);
  };

  return (
    <>
      {/* Właściwy <Tr> dla poprawnego układu tabeli */}
      <Tr
        ref={setSortableRef}
        style={workSortableStyle}
        {...sortableAttrs}
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
      >
        {/* ── Sticky lewa kolumna ── */}
        <Td
          position="sticky"
          left={0}
          zIndex={2}
          width={`${treeColumnWidth}px`}
          minW={`${treeColumnWidth}px`}
          maxW={`${treeColumnWidth}px`}
          p={0}
          bg={rowBg}
          borderBottomWidth="1px"
          borderBottomColor={borderColor}
        >
          <Box
            display="flex"
            alignItems="center"
            h={`${rowHeight}px`}
            pl={`${8 + depth * DEPTH_INDENT}px`}
            pr={2}
            gap={1}
            style={{ userSelect: "none" }}
          >
            {isEditing && (
              <Box
                ref={setSortableActivatorRef}
                {...sortableListeners}
                color="gray.400"
                cursor="grab"
                flexShrink={0}
                _active={{ cursor: "grabbing" }}
              >
                <GripVertical size={12} />
              </Box>
            )}

            {/* Checkbox IsClosed z logiką propagacji */}
            <Checkbox
              isChecked={closedState === "checked"}
              isIndeterminate={closedState === "indeterminate"}
              colorScheme="green"
              size="sm"
              isDisabled={isTogglingClosed}
              onChange={handleToggleClosed}
              flexShrink={0}
            />

            {/* Kolorowy indicator */}
            <Box
              w="10px"
              h="10px"
              borderRadius="full"
              bg={work.colorRgb}
              flexShrink={0}
              flexGrow={0}
            />

            {/* Nazwa */}
            <Box flex={1} minW={0}>
              {isEditingName ? (
                <Input
                  autoFocus
                  value={nameInput}
                  onChange={handleNameChange}
                  onBlur={commitName}
                  onKeyDown={handleNameKeyDown}
                  size="xs"
                  isDisabled={isRenaming}
                />
              ) : (
                <GanttTruncatedName
                  label={work.name}
                  fontSize="sm"
                  color={work.isClosed ? "gray.500" : undefined}
                  textDecoration={work.isClosed ? "line-through" : "none"}
                  cursor={isEditing ? "text" : "default"}
                  onClick={handleClick}
                  isEditingMode={isEditing}
                  editHint="Kliknij aby zmienić nazwę"
                />
              )}
            </Box>

            {isRenaming && <Spinner size="xs" flexShrink={0} />}

            {/* Komentarze — badge toggle */}
            {commentCount > 0 && (
              <Tooltip label={`${commentCount} komentarz(y) — kliknij aby ${showInlineComments ? "ukryć" : "pokazać"}`}>
                <Badge
                  colorScheme="primary"
                  variant="subtle"
                  fontSize="2xs"
                  flexShrink={0}
                  cursor="pointer"
                  onClick={() => setShowInlineComments(v => !v)}
                >
                  <HStack spacing={1}>
                    <MessageSquare size={9} />
                    <span>{commentCount}</span>
                  </HStack>
                </Badge>
              </Tooltip>
            )}

            {/* Avatary przypisanych — zawsze widoczne */}
            <Popover placement="bottom-end">
              <PopoverTrigger>
                <Box cursor={isEditing ? "pointer" : "default"} flexShrink={0}>
                  {(selectedUserIds.length + selectedContractorIds.length) > 0 ? (
                    <AvatarGroup size="xs" max={3}>
                      {members
                        .filter(m => selectedUserIds.includes(m.userId))
                        .map(m => (
                          <Tooltip key={m.userId} label={getMemberDisplayName(m)}>
                            <Avatar name={getMemberDisplayName(m)} size="xs" />
                          </Tooltip>
                        ))}
                      {contractors
                        .filter(c => selectedContractorIds.includes(c.id))
                        .map(c => (
                          <Tooltip key={c.id} label={c.name}>
                            <Avatar name={c.name} size="xs" />
                          </Tooltip>
                        ))}
                    </AvatarGroup>
                  ) : isEditing ? (
                    <Tooltip label="Przypisz osobę">
                      <IconButton
                        aria-label="Przypisz osobę"
                        icon={<UserPlus size={12} />}
                        size="xs"
                        variant="ghost"
                        colorScheme="gray"
                        flexShrink={0}
                      />
                    </Tooltip>
                  ) : null}
                </Box>
              </PopoverTrigger>
              {isEditing && (
                <PopoverContent w="240px" zIndex={1500}>
                  <PopoverArrow />
                  <PopoverBody>
                    <VStack align="start" spacing={1}>
                      <Text fontSize="xs" fontWeight="semibold" mb={1}>Zespół projektu</Text>
                      {members.map(m => (
                        <Checkbox
                          key={m.userId}
                          isChecked={selectedUserIds.includes(m.userId)}
                          onChange={() => handleToggleAssignee(m.userId)}
                          size="sm"
                        >
                          <Text fontSize="xs">{getMemberDisplayName(m)}</Text>
                        </Checkbox>
                      ))}
                      <Text fontSize="xs" fontWeight="semibold" mb={1} mt={2}>Kontahenci</Text>
                      {contractors.map(c => (
                        <Checkbox
                          key={c.id}
                          isChecked={selectedContractorIds.includes(c.id)}
                          onChange={() => handleToggleContractor(c.id)}
                          size="sm"
                        >
                          <Text fontSize="xs">{c.name}</Text>
                        </Checkbox>
                      ))}
                      {contractors.length === 0 && (
                        <Text fontSize="xs" color="neutral.400">Brak kontahentów</Text>
                      )}
                    </VStack>
                  </PopoverBody>
                </PopoverContent>
              )}
            </Popover>

            {/* Akcja usuń — zawsze widoczna w trybie edycji */}
            {isEditing && !isEditingName && (
              <Tooltip label="Usuń zakres pracy">
                <IconButton
                  aria-label="Usuń"
                  icon={isDeleting ? <Spinner size="xs" /> : <Trash2 size={12} />}
                  size="xs"
                  variant="ghost"
                  colorScheme="red"
                  isDisabled={isDeleting}
                  onClick={onDeleteOpen}
                  flexShrink={0}
                />
              </Tooltip>
            )}
          </Box>
        </Td>

        {/* ── Prawa kolumna: jeden Td z belkami Gantta ── */}
        <Td
          colSpan={dates.length}
          p={0}
          position="relative"
          height={`${rowHeight}px`}
          bg={rowBg}
          borderBottomWidth="1px"
          borderBottomColor={borderColor}
          cursor={isEditing ? "cell" : "default"}
          style={{
            backgroundImage: `repeating-linear-gradient(to right, transparent 0px, transparent calc(${columnWidth}px - 1px), ${gridLineColor} calc(${columnWidth}px - 1px), ${gridLineColor} ${columnWidth}px)`,
            overflow: "visible",
          }}
          onMouseDown={handleTimelineMD}
          onMouseMove={handleTimelineMM}
          onMouseUp={handleTimelineMU}
          onMouseLeave={handleTimelineMU}
        >
          {/* Podgląd zaznaczenia nowego okresu */}
          {cellSelection && (
            <Box
              position="absolute"
              top="20%"
              height="60%"
              bg={selectionBg}
              opacity={0.6}
              borderRadius="sm"
              pointerEvents="none"
              left={`${Math.min(cellSelection.startIdx, cellSelection.endIdx) * columnWidth}px`}
              width={`${(Math.abs(cellSelection.endIdx - cellSelection.startIdx) + 1) * columnWidth}px`}
            />
          )}
          {(work.periods ?? []).map(period => {
            const startIdx = findColIdx(dates, period.startDate);
            if (startIdx < 0) return null;
            const endIdx = findColIdx(dates, period.endDate);
            const colSpan = Math.max((endIdx >= 0 ? endIdx : dates.length - 1) - startIdx + 1, 1);
            return (
              <GanttBar
                key={period.id}
                work={work}
                stageId={stageId}
                periods={[period]}
                dates={dates}
                colMap={makeDateColMap(dates)}
                columnWidth={columnWidth}
                rowHeight={rowHeight}
              />
            );
          })}
        </Td>
      </Tr>

      {/* Rozwijalne komentarze pod wierszem */}
      {showInlineComments && commentCount > 0 && (
        <Tr>
          <Td
            colSpan={dates.length + 1}
            p={2}
            bg={commentsBg}
            borderBottomWidth="1px"
            borderBottomColor={borderColor}
          >
            <GanttCommentPopover workId={work.id} stageId={stageId} />
          </Td>
        </Tr>
      )}

      {/* Dialog potwierdzenia usunięcia */}
      <AlertDialog isOpen={isDeleteOpen} leastDestructiveRef={cancelRef} onClose={onDeleteClose}>
        <AlertDialogOverlay>
          <AlertDialogContent>
            <AlertDialogHeader>Usuń zakres pracy</AlertDialogHeader>
            <AlertDialogBody>
              Czy na pewno chcesz usunąć zakres pracy <strong>{work.name}</strong>?
            </AlertDialogBody>
            <AlertDialogFooter>
              <Button ref={cancelRef} onClick={onDeleteClose}>Anuluj</Button>
              <Button colorScheme="red" onClick={handleDeleteConfirm} ml={3}>Usuń</Button>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialogOverlay>
      </AlertDialog>
    </>
  );
}
