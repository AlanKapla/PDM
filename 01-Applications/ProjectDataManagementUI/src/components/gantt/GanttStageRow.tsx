import { useState, useRef, useCallback } from "react";
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
  Spinner,
  Tr,
  Td,
} from "@chakra-ui/react";
import { ChevronDown, ChevronRight, Plus, Trash2, GripVertical } from "lucide-react";
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
  useSortable,
} from "@dnd-kit/sortable";
import { useGantt } from "./GanttContext";
import GanttWorkRow from "./GanttWorkRow";
import type { WorkScheduleStageWeb } from "../../types/workSchedule.types";

// ─── Stałe ────────────────────────────────────────────────────────────────────
const DEPTH_INDENT = 20; // px wcięcia na poziom zagnieżdżenia

interface GanttStageRowProps {
  stage: WorkScheduleStageWeb;
  depth: number;
  dates: Date[];
  columnWidth: number;
  rowHeight: number;
  treeColumnWidth: number;
}

export default function GanttStageRow({
  stage, depth, dates, columnWidth, rowHeight, treeColumnWidth,
}: GanttStageRowProps) {
  const {
    mode, expandedStages, toggleStage, renameStage, deleteStage, addWork, addStage, reorderWorks, isMutating,
  } = useGantt();

  const { isOpen: isDeleteOpen, onOpen: onDeleteOpen, onClose: onDeleteClose } = useDisclosure();
  const [isEditingName, setIsEditingName] = useState(false);
  const [nameInput, setNameInput] = useState(stage.name);
  const cancelRef = useRef<HTMLButtonElement>(null);
  const [isHovered, setIsHovered] = useState(false);
  const [isAddingWork, setIsAddingWork] = useState(false);
  const [newWorkName, setNewWorkName] = useState("");
  const renameDebounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const [newWorkColor, setNewWorkColor] = useState("#3182CE");
  const newWorkInputRef = useRef<HTMLInputElement>(null);

  const isExpanded = expandedStages.has(stage.id);
  const childStages = (stage.childStages ?? []).slice().sort((a, b) => a.order - b.order);
  const works = (stage.works ?? []).slice().sort((a, b) => a.order - b.order);
  const isEditing = mode === "edit";
  const isDeleting = isMutating.has(`deleteStage-${stage.id}`);
  const isRenaming = isMutating.has(`renameStage-${stage.id}`);
  const hasCostEstimate = !!stage.costEstimateGroupId;

  const bgStage = useColorModeValue("primary.50", "primary.900");
  const bgStageHover = useColorModeValue("primary.100", "primary.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const stageBorderLColor = useColorModeValue("primary.400", "primary.600");
  const addingWorkBg = useColorModeValue("green.50", "green.900");

  // ─── DnD dla etapu ──────────────────────────────────────────────────────────
  const {
    attributes: sortableAttrs,
    listeners: sortableListeners,
    setNodeRef: setSortableRef,
    setActivatorNodeRef: setSortableActivatorRef,
    transform: sortableTransform,
    transition: sortableTransition,
    isDragging: isSortableDragging,
  } = useSortable({ id: stage.id });

  const stageSortableStyle: React.CSSProperties = {
    transform: sortableTransform
      ? `translate3d(${sortableTransform.x}px, ${sortableTransform.y}px, 0)`
      : undefined,
    transition: sortableTransition ?? undefined,
    opacity: isSortableDragging ? 0.5 : 1,
  };

  // ─── DnD dla zakresów pracy wewnątrz etapu ──────────────────────────────────
  const workSensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 8 } })
  );
  const workIds = works.map(w => w.id);

  const handleWorkDragEnd = useCallback((event: DragEndEvent) => {
    const { active, over } = event;
    if (!over || active.id === over.id) return;
    const oldIndex = workIds.indexOf(String(active.id));
    const newIndex = workIds.indexOf(String(over.id));
    if (oldIndex < 0 || newIndex < 0) return;
    reorderWorks(stage.id, arrayMove(workIds, oldIndex, newIndex));
  }, [workIds, stage.id, reorderWorks]);

  // ─── Rename z debounce ───────────────────────────────────────────────────────
  const handleDoubleClick = () => {
    if (!isEditing) return;
    setNameInput(stage.name);
    setIsEditingName(true);
  };

  const handleNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    setNameInput(value);
    if (renameDebounceRef.current) clearTimeout(renameDebounceRef.current);
    renameDebounceRef.current = setTimeout(async () => {
      const trimmed = value.trim();
      if (!trimmed || trimmed === stage.name) return;
      await renameStage(stage.id, trimmed);
    }, 700);
  };

  const commitName = async () => {
    setIsEditingName(false);
    if (renameDebounceRef.current) {
      clearTimeout(renameDebounceRef.current);
      renameDebounceRef.current = null;
    }
    const trimmed = nameInput.trim();
    if (!trimmed || trimmed === stage.name) return;
    await renameStage(stage.id, trimmed);
  };

  const handleNameKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Enter") (e.target as HTMLInputElement).blur();
    if (e.key === "Escape") {
      setIsEditingName(false);
      if (renameDebounceRef.current) clearTimeout(renameDebounceRef.current);
      setNameInput(stage.name);
    }
  };

  const handleDeleteConfirm = async () => {
    onDeleteClose();
    await deleteStage(stage.id);
  };

  const handleAddWorkSubmit = async () => {
    const name = newWorkName.trim();
    if (!name) return;
    await addWork(stage.id, name, newWorkColor);
    setIsAddingWork(false);
    setNewWorkName("");
  };

  const handleAddWorkKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Enter") handleAddWorkSubmit();
    if (e.key === "Escape") { setIsAddingWork(false); setNewWorkName(""); }
  };

  const startAddWork = () => {
    setIsAddingWork(true);
    setNewWorkName("");
    setTimeout(() => newWorkInputRef.current?.focus(), 50);
  };

  // ─── Render ─────────────────────────────────────────────────────────────────
  return (
    <>
      {/* Wiersz etapu — sortable */}
      <Tr
        ref={setSortableRef}
        style={stageSortableStyle}
        {...sortableAttrs}
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
      >
        {/* Sticky lewa kolumna: nazwa etapu */}
        <Td
          position="sticky"
          left={0}
          zIndex={2}
          width={`${treeColumnWidth}px`}
          minW={`${treeColumnWidth}px`}
          maxW={`${treeColumnWidth}px`}
          p={0}
          bg={isHovered ? bgStageHover : bgStage}
          borderLeftWidth="3px"
          borderLeftColor={stageBorderLColor}
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
            cursor="default"
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
                <GripVertical size={14} />
              </Box>
            )}

            {/* Rozwiń / zwiń */}
            <IconButton
              aria-label={isExpanded ? "Zwiń" : "Rozwiń"}
              icon={isExpanded ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
              size="xs"
              variant="ghost"
              colorScheme="primary"
              onClick={() => toggleStage(stage.id)}
              flexShrink={0}
            />

            {/* Nazwa — dwuklik w trybie edycji */}
            <Box flex={1} minW={0}>
              {isEditingName ? (
                <Input
                  id={`stage-name-input-${stage.id}`}
                  value={nameInput}
                  onChange={handleNameChange}
                  onBlur={commitName}
                  onKeyDown={handleNameKeyDown}
                  size="xs"
                  fontWeight="semibold"
                  isDisabled={isRenaming}
                />
              ) : (
                <Text
                  fontSize="sm"
                  fontWeight="semibold"
                  noOfLines={1}
                  onClick={handleDoubleClick}
                  title={isEditing ? "Kliknij aby zmienić nazwę" : stage.name}
                >
                  {stage.name || <Text as="span" color="gray.400" fontStyle="italic">Bez nazwy</Text>}
                </Text>
              )}
            </Box>

            {/* Odznaki */}
            {hasCostEstimate && (
              <Badge colorScheme="level2" fontSize="2xs" variant="subtle" flexShrink={0}>KOSZTORYS</Badge>
            )}
            <Badge colorScheme="gray" variant="subtle" fontSize="2xs" flexShrink={0}>
              {works.length > 0 ? `${works.length} PRACE` : "BRAK"}
            </Badge>

            {isRenaming && <Spinner size="xs" flexShrink={0} />}

            {/* Akcje hover (tryb edycji) */}
            {isEditing && isHovered && !isEditingName && (
              <HStack spacing={1} flexShrink={0}>
                <Tooltip label="+ Zakres pracy">
                  <IconButton
                    aria-label="Dodaj zakres pracy"
                    icon={<Plus size={12} />}
                    size="xs"
                    variant="ghost"
                    colorScheme="green"
                    onClick={startAddWork}
                  />
                </Tooltip>
                <Tooltip label="+ Podetap">
                  <IconButton
                    aria-label="Dodaj podetap"
                    icon={<Plus size={12} />}
                    size="xs"
                    variant="ghost"
                    colorScheme="primary"
                    onClick={() => addStage("Nowy podetap", stage.id)}
                  />
                </Tooltip>
                <Tooltip label="Usuń etap">
                  <IconButton
                    aria-label="Usuń etap"
                    icon={isDeleting ? <Spinner size="xs" /> : <Trash2 size={12} />}
                    size="xs"
                    variant="ghost"
                    colorScheme="red"
                    isDisabled={isDeleting}
                    onClick={onDeleteOpen}
                  />
                </Tooltip>
              </HStack>
            )}
          </Box>
        </Td>

        {/* Komórki timeline (puste dla wiersza etapu) */}
        {dates.map((_, idx) => (
          <Td
            key={idx}
            width={`${columnWidth}px`}
            minW={`${columnWidth}px`}
            p={0}
            bg={isHovered ? bgStageHover : bgStage}
            borderBottomWidth="1px"
            borderBottomColor={borderColor}
            borderRightWidth="1px"
            borderRightColor={borderColor}
          />
        ))}
      </Tr>

      {/* Rozwinięte dzieci */}
      {isExpanded && (
        <>
          {/* Zakresy prac — z DnD sortable */}
          {works.length > 0 && (
            <DndContext
              sensors={workSensors}
              collisionDetection={closestCenter}
              onDragEnd={handleWorkDragEnd}
            >
              <SortableContext items={workIds} strategy={verticalListSortingStrategy}>
                {works.map(work => (
                  <GanttWorkRow
                    key={work.id}
                    work={work}
                    stageId={stage.id}
                    depth={depth + 1}
                    dates={dates}
                    columnWidth={columnWidth}
                    rowHeight={rowHeight}
                    treeColumnWidth={treeColumnWidth}
                  />
                ))}
              </SortableContext>
            </DndContext>
          )}

          {/* Formularz dodania nowego zakresu pracy inline */}
          {isAddingWork && (
            <Tr>
              <Td
                position="sticky"
                left={0}
                zIndex={2}
                width={`${treeColumnWidth}px`}
                minW={`${treeColumnWidth}px`}
                p={0}
                bg={addingWorkBg}
                borderBottomWidth="1px"
                borderBottomColor={borderColor}
              >
                <Box
                  display="flex"
                  alignItems="center"
                  h="36px"
                  pl={`${8 + (depth + 1) * DEPTH_INDENT}px`}
                  pr={2}
                  gap={2}
                >
                  <Box
                    w="12px"
                    h="12px"
                    borderRadius="full"
                    bg={newWorkColor}
                    flexShrink={0}
                    cursor="pointer"
                    title="Kliknij aby zmienić kolor"
                  />
                  <Input
                    ref={newWorkInputRef}
                    value={newWorkName}
                    onChange={e => setNewWorkName(e.target.value)}
                    onKeyDown={handleAddWorkKeyDown}
                    placeholder="Nazwa zakresu pracy..."
                    size="xs"
                    flex={1}
                  />
                  <input
                    type="color"
                    value={newWorkColor}
                    onChange={e => setNewWorkColor(e.target.value)}
                    style={{ width: 24, height: 24, border: "none", padding: 0, cursor: "pointer", borderRadius: 4 }}
                    title="Kolor"
                  />
                  <Button size="xs" colorScheme="green" onClick={handleAddWorkSubmit}>Dodaj</Button>
                  <Button size="xs" variant="ghost" onClick={() => setIsAddingWork(false)}>Anuluj</Button>
                </Box>
              </Td>
              {dates.map((_, idx) => (
                <Td
                  key={idx}
                  p={0}
                  bg={addingWorkBg}
                  borderBottomWidth="1px"
                  borderBottomColor={borderColor}
                />
              ))}
            </Tr>
          )}

          {/* Podetapy (rekurencja) */}
          {childStages.map(child => (
            <GanttStageRow
              key={child.id}
              stage={child}
              depth={depth + 1}
              dates={dates}
              columnWidth={columnWidth}
              rowHeight={rowHeight}
              treeColumnWidth={treeColumnWidth}
            />
          ))}
        </>
      )}

      {/* Dialog potwierdzenia usunięcia */}
      <AlertDialog isOpen={isDeleteOpen} leastDestructiveRef={cancelRef} onClose={onDeleteClose}>
        <AlertDialogOverlay>
          <AlertDialogContent>
            <AlertDialogHeader>Usuń etap</AlertDialogHeader>
            <AlertDialogBody>
              Czy na pewno chcesz usunąć etap <strong>{stage.name}</strong> i wszystkie jego zakresy pracy?
              Tej operacji nie można cofnąć.
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
