import React, { useEffect, useRef, useState } from "react";
import {
  Box,
  Button,
  Divider,
  HStack,
  IconButton,
  Menu,
  MenuButton,
  MenuDivider,
  MenuItem,
  MenuList,
  Tooltip,
} from "@chakra-ui/react";
import {
  CalendarDays,
  ChevronDown,
  ChevronUp,
  Edit,
  Eye,
  EyeOff,
  FileSpreadsheet,
  MessageSquare,
  MoreHorizontal,
  Pencil,
  Plus,
  RefreshCw,
  Save,
  X,
  Zap,
} from "lucide-react";

// ─── Types ────────────────────────────────────────────────────────────────────

type ToolbarBp = "full" | "compact" | "mobile";

export interface WorkScheduleToolbarProps {
  // State
  hasCostEstimate: boolean;
  showComments: boolean;
  hideWeekends: boolean;
  isEditing: boolean;
  isDirty: boolean;
  isSyncing: boolean;
  canEdit: boolean;

  // Handlers
  onNavigateToCostEstimate: () => void;
  onToggleComments: () => void;
  onSyncFromCostEstimate: () => void;
  onAddStage: () => void;
  onEditMode: () => void;
  onToggleInlineEdit: () => void;
  onSaveAndExitEdit: () => void;
  onCancelEdit: () => void;
  onToggleWeekends: () => void;
  onScrollToToday: () => void;
  onExpandAll: () => void;
  onCollapseAll: () => void;
}

interface ActionDef {
  id: string;
  icon: React.ReactElement;
  label: string;
  tooltip: string;
  onClick: () => void;
  isActive?: boolean;
  colorScheme?: string;
  variant?: string;
  isLoading?: boolean;
  isVisible: boolean;
}

// ─── Active dot indicator ─────────────────────────────────────────────────────

function ActiveDot() {
  return (
    <Box
      as="span"
      position="absolute"
      top="-2px"
      right="-2px"
      w="7px"
      h="7px"
      borderRadius="full"
      bg="teal.400"
      border="1.5px solid"
      borderColor="white"
      zIndex={1}
      pointerEvents="none"
    />
  );
}

// ─── Component ────────────────────────────────────────────────────────────────

export default function WorkScheduleToolbar({
  hasCostEstimate,
  showComments,
  hideWeekends,
  isEditing,
  isDirty,
  isSyncing,
  canEdit,
  onNavigateToCostEstimate,
  onToggleComments,
  onSyncFromCostEstimate,
  onAddStage,
  onEditMode,
  onToggleInlineEdit,
  onSaveAndExitEdit,
  onCancelEdit,
  onToggleWeekends,
  onScrollToToday,
  onExpandAll,
  onCollapseAll,
}: WorkScheduleToolbarProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [bp, setBp] = useState<ToolbarBp>("full");

  // Detect container width via ResizeObserver (nie window.resize)
  useEffect(() => {
    const el = containerRef.current;
    if (!el) return;

    const measure = (w: number) =>
      setBp(w >= 1100 ? "full" : w >= 600 ? "compact" : "mobile");

    // Odczyt synchroniczny przy montowaniu
    measure(el.offsetWidth);

    const obs = new ResizeObserver(([entry]) =>
      measure(entry.contentRect.width)
    );
    obs.observe(el);
    return () => obs.disconnect();
  }, []);

  // ─── Action definitions ───────────────────────────────────────────────────

  const viewActions: ActionDef[] = [
    {
      id: "weekends",
      icon: hideWeekends ? <Eye size={14} /> : <EyeOff size={14} />,
      label: hideWeekends ? "Pokaż weekendy" : "Ukryj weekendy",
      tooltip: hideWeekends ? "Pokaż weekendy" : "Ukryj weekendy",
      onClick: onToggleWeekends,
      isActive: hideWeekends,
      colorScheme: "gray",
      variant: hideWeekends ? "solid" : "outline",
      isVisible: true,
    },
    {
      id: "today",
      icon: <CalendarDays size={14} />,
      label: "Dzisiaj",
      tooltip: "Przewiń do dzisiejszej daty",
      onClick: onScrollToToday,
      colorScheme: "blue",
      variant: "outline",
      isVisible: true,
    },
    {
      id: "expand",
      icon: <ChevronDown size={14} />,
      label: "Rozwiń",
      tooltip: "Rozwiń wszystkie etapy",
      onClick: onExpandAll,
      colorScheme: "gray",
      variant: "outline",
      isVisible: true,
    },
    {
      id: "collapse",
      icon: <ChevronUp size={14} />,
      label: "Zwiń",
      tooltip: "Zwiń wszystkie etapy",
      onClick: onCollapseAll,
      colorScheme: "gray",
      variant: "outline",
      isVisible: true,
    },
  ];

  const editActions: ActionDef[] = [
    {
      id: "edit-modal",
      icon: <Edit size={14} />,
      label: "Edytuj",
      tooltip: "Otwórz formularz edycji harmonogramu",
      onClick: onEditMode,
      colorScheme: "blue",
      variant: "outline",
      isVisible: canEdit && !isEditing,
    },
    {
      id: "inline-edit",
      icon: <Pencil size={14} />,
      label: isEditing ? "Edycja inline ✓" : "Edycja inline",
      tooltip: isEditing ? "Wyjdź z trybu edycji" : "Włącz edycję inline",
      onClick: onToggleInlineEdit,
      isActive: isEditing,
      colorScheme: isEditing ? "teal" : "gray",
      variant: isEditing ? "solid" : "outline",
      isVisible: canEdit,
    },
    {
      id: "add-stage",
      icon: <Plus size={14} />,
      label: "Dodaj etap",
      tooltip: "Dodaj nowy etap do harmonogramu",
      onClick: onAddStage,
      colorScheme: "gray",
      variant: "outline",
      isVisible: canEdit && isEditing,
    },
    {
      id: "save",
      icon: <Save size={14} />,
      label: "Zapisz",
      tooltip: "Zapisz zmiany i wyjdź z trybu edycji",
      onClick: onSaveAndExitEdit,
      colorScheme: "teal",
      variant: "solid",
      isVisible: canEdit && (isEditing || isDirty),
    },
    {
      id: "cancel",
      icon: <X size={14} />,
      label: "Anuluj",
      tooltip: "Odrzuć niezapisane zmiany",
      onClick: onCancelEdit,
      colorScheme: "gray",
      variant: "ghost",
      isVisible: canEdit && (isEditing || isDirty),
    },
  ];

  const akcjeActions: ActionDef[] = [
    {
      id: "cost-estimate",
      icon: <FileSpreadsheet size={14} />,
      label: "Kosztorys",
      tooltip: "Przejdź do powiązanego kosztorysu",
      onClick: onNavigateToCostEstimate,
      colorScheme: "gray",
      variant: "ghost",
      isVisible: hasCostEstimate,
    },
    {
      id: "comments",
      icon: <MessageSquare size={14} />,
      label: "Komentarze",
      tooltip: showComments ? "Ukryj komentarze do prac" : "Pokaż komentarze do prac",
      onClick: onToggleComments,
      isActive: showComments,
      colorScheme: showComments ? "teal" : "gray",
      variant: showComments ? "solid" : "ghost",
      isVisible: true,
    },
    {
      id: "sync",
      icon: <RefreshCw size={14} />,
      label: "Odśwież z kosztorysu",
      tooltip: "Aktualizuje strukturę etapów na podstawie grup w kosztorysie",
      onClick: onSyncFromCostEstimate,
      isLoading: isSyncing,
      colorScheme: "gray",
      variant: "ghost",
      isVisible: hasCostEstimate && canEdit && !isEditing,
    },
  ];

  // Aktywne stany grup (dla wskaźnika w trybie zwinięcia)
  const hasActiveEdit = isEditing || isDirty;
  const hasActiveView = hideWeekends;
  const hasActiveAction = showComments;

  // ─── Render helpers ───────────────────────────────────────────────────────

  /** Przycisk z etykietą (tryb pełny) */
  const renderFullButton = (a: ActionDef) => {
    if (!a.isVisible) return null;
    return (
      <Tooltip key={a.id} label={a.tooltip} hasArrow placement="bottom" openDelay={400}>
        <Button
          leftIcon={a.icon}
          size="sm"
          colorScheme={a.colorScheme ?? "gray"}
          variant={a.variant ?? "outline"}
          isLoading={a.isLoading}
          onClick={a.onClick}
        >
          {a.label}
        </Button>
      </Tooltip>
    );
  };

  /** Ikona z tooltipem (tryb compact) */
  const renderCompactButton = (a: ActionDef) => {
    if (!a.isVisible) return null;
    return (
      <Tooltip key={a.id} label={a.tooltip} hasArrow placement="bottom">
        <IconButton
          aria-label={a.label}
          icon={a.icon}
          size="sm"
          colorScheme={a.colorScheme ?? "gray"}
          variant={a.variant ?? "outline"}
          isLoading={a.isLoading}
          onClick={a.onClick}
        />
      </Tooltip>
    );
  };

  /** Element listy dropdown */
  const renderMenuItem = (a: ActionDef) => {
    if (!a.isVisible) return null;
    return (
      <MenuItem
        key={a.id}
        icon={a.icon}
        onClick={a.onClick}
        fontWeight={a.isActive ? "semibold" : "normal"}
        color={a.isActive ? "teal.600" : undefined}
      >
        {a.label}
      </MenuItem>
    );
  };

  // ─── Layouts ──────────────────────────────────────────────────────────────

  const visibleView = viewActions.filter((a) => a.isVisible);
  const visibleEdit = editActions.filter((a) => a.isVisible);
  const visibleAkcje = akcjeActions.filter((a) => a.isVisible);

  const hasViewSep = visibleView.length > 0 && visibleEdit.length > 0;
  const hasEditSep = visibleEdit.length > 0 && visibleAkcje.length > 0;

  return (
    <Box ref={containerRef} w="100%">
      {/* ── FULL (≥1100px): wszystkie przyciski z etykietami ── */}
      {bp === "full" && (
        <HStack spacing={2} flexWrap="wrap">
          {visibleView.map(renderFullButton)}
          {hasViewSep && (
            <Divider orientation="vertical" height="20px" alignSelf="center" />
          )}
          {visibleEdit.map(renderFullButton)}
          {hasEditSep && (
            <Divider orientation="vertical" height="20px" alignSelf="center" />
          )}
          {visibleAkcje.map(renderFullButton)}
        </HStack>
      )}

      {/* ── COMPACT (600–1099px): widok+edycja jako ikony, akcje → dropdown ── */}
      {bp === "compact" && (
        <HStack spacing={2} flexWrap="wrap">
          {visibleView.map(renderCompactButton)}
          {hasViewSep && (
            <Divider orientation="vertical" height="20px" alignSelf="center" />
          )}
          {visibleEdit.map(renderCompactButton)}
          {visibleAkcje.length > 0 && (
            <>
              {hasEditSep && (
                <Divider orientation="vertical" height="20px" alignSelf="center" />
              )}
              <Box position="relative" display="inline-flex">
                <Menu>
                  <Tooltip label="Akcje" hasArrow placement="bottom" isDisabled>
                    <MenuButton
                      as={IconButton}
                      icon={<MoreHorizontal size={14} />}
                      size="sm"
                      variant="outline"
                      colorScheme={hasActiveAction ? "teal" : "gray"}
                      aria-label="Akcje"
                    />
                  </Tooltip>
                  <MenuList minW="200px">
                    {visibleAkcje.map(renderMenuItem)}
                  </MenuList>
                </Menu>
                {hasActiveAction && <ActiveDot />}
              </Box>
            </>
          )}
        </HStack>
      )}

      {/* ── MOBILE (<600px): 3 grupy jako osobne przyciski z dropdown ── */}
      {bp === "mobile" && (
        <HStack spacing={2} flexWrap="wrap">
          {/* Widok */}
          {visibleView.length > 0 && (
            <Box position="relative" display="inline-flex">
              <Menu>
                <MenuButton
                  as={Button}
                  rightIcon={<ChevronDown size={12} />}
                  leftIcon={<Eye size={13} />}
                  size="xs"
                  colorScheme={hasActiveView ? "teal" : "gray"}
                  variant={hasActiveView ? "solid" : "outline"}
                >
                  Widok
                </MenuButton>
                <MenuList minW="200px">
                  {visibleView.map(renderMenuItem)}
                </MenuList>
              </Menu>
              {hasActiveView && <ActiveDot />}
            </Box>
          )}

          {/* Edycja */}
          {visibleEdit.length > 0 && (
            <Box position="relative" display="inline-flex">
              <Menu>
                <MenuButton
                  as={Button}
                  rightIcon={<ChevronDown size={12} />}
                  leftIcon={<Edit size={13} />}
                  size="xs"
                  colorScheme={hasActiveEdit ? "teal" : "gray"}
                  variant={hasActiveEdit ? "solid" : "outline"}
                >
                  Edycja
                </MenuButton>
                <MenuList minW="200px">
                  {visibleEdit.map(renderMenuItem)}
                </MenuList>
              </Menu>
              {hasActiveEdit && <ActiveDot />}
            </Box>
          )}

          {/* Akcje */}
          {visibleAkcje.length > 0 && (
            <Box position="relative" display="inline-flex">
              <Menu>
                <MenuButton
                  as={Button}
                  rightIcon={<ChevronDown size={12} />}
                  leftIcon={<Zap size={13} />}
                  size="xs"
                  colorScheme={hasActiveAction ? "teal" : "gray"}
                  variant={hasActiveAction ? "solid" : "outline"}
                >
                  Akcje
                </MenuButton>
                <MenuList minW="200px">
                  {visibleAkcje.map(renderMenuItem)}
                </MenuList>
              </Menu>
              {hasActiveAction && <ActiveDot />}
            </Box>
          )}
        </HStack>
      )}
    </Box>
  );
}
