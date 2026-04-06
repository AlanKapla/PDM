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
  ChevronsDown,
  ChevronsUp,
  Edit,
  Eye,
  MoreHorizontal,
  RefreshCw,
  Save,
  Share2,
  X,
  Zap,
} from "lucide-react";

// ─── Types ────────────────────────────────────────────────────────────────────

type ToolbarBp = "full" | "compact" | "mobile";

export interface CostEstimateToolbarProps {
  // Stan
  isEditMode: boolean;
  hasChanges: boolean;
  canEdit: boolean;
  canShare: boolean;
  hasSchedule: boolean;
  isSyncing: boolean;
  isRecalculating: boolean;

  // Handlery
  onExpandAll: () => void;
  onCollapseAll: () => void;
  onSetViewMode: () => void;
  onSetEditMode: () => void;
  onSave: () => void;
  onCancelEdit: () => void;
  onNavigateToSchedule: () => void;
  onCreateSchedule: () => void;
  onSyncSchedule: () => void;
  onShare: () => void;
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

// ─── Wskaźnik aktywności grupy ────────────────────────────────────────────────

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
      bg="action.400"
      border="1.5px solid"
      borderColor="white"
      zIndex={1}
      pointerEvents="none"
    />
  );
}

// ─── Komponent ────────────────────────────────────────────────────────────────

export default function CostEstimateToolbar({
  isEditMode,
  hasChanges,
  canEdit,
  canShare,
  hasSchedule,
  isSyncing,
  isRecalculating,
  onExpandAll,
  onCollapseAll,
  onSetViewMode,
  onSetEditMode,
  onSave,
  onCancelEdit,
  onNavigateToSchedule,
  onCreateSchedule,
  onSyncSchedule,
  onShare,
}: CostEstimateToolbarProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [bp, setBp] = useState<ToolbarBp>("full");

  // Responsywność przez ResizeObserver – identyczny wzorzec jak w WorkScheduleToolbar
  useEffect(() => {
    const el = containerRef.current;
    if (!el) return;

    const measure = (w: number) =>
      setBp(w >= 1100 ? "full" : w >= 600 ? "compact" : "mobile");

    measure(el.offsetWidth);

    const obs = new ResizeObserver(([entry]) =>
      measure(entry.contentRect.width)
    );
    obs.observe(el);
    return () => obs.disconnect();
  }, []);

  // ─── Definicje akcji ─────────────────────────────────────────────────────

  const viewActions: ActionDef[] = [
    {
      id: "expand",
      icon: <ChevronsDown size={14} />,
      label: "Rozwiń wszystko",
      tooltip: "Rozwiń wszystkie etapy",
      onClick: onExpandAll,
      colorScheme: "gray",
      variant: "outline",
      isVisible: true,
    },
    {
      id: "collapse",
      icon: <ChevronsUp size={14} />,
      label: "Zwiń wszystko",
      tooltip: "Zwiń wszystkie etapy",
      onClick: onCollapseAll,
      colorScheme: "gray",
      variant: "outline",
      isVisible: true,
    },
    {
      id: "view-mode",
      icon: <Eye size={14} />,
      label: "Podgląd",
      tooltip: "Przełącz w tryb podglądu (tylko odczyt)",
      onClick: onSetViewMode,
      isActive: !isEditMode,
      colorScheme: !isEditMode ? "blue" : "gray",
      variant: !isEditMode ? "solid" : "outline",
      isVisible: canEdit,
    },
  ];

  const editActions: ActionDef[] = [
    {
      id: "edit-mode",
      icon: <Edit size={14} />,
      label: "Edycja",
      tooltip: "Włącz tryb edycji inline",
      onClick: onSetEditMode,
      isActive: isEditMode,
      colorScheme: isEditMode ? "green" : "gray",
      variant: isEditMode ? "solid" : "outline",
      isVisible: canEdit,
    },
    {
      id: "save",
      icon: <Save size={14} />,
      label: "Zapisz",
      tooltip: "Przelicz i zapisz kosztorys",
      onClick: onSave,
      colorScheme: "teal",
      variant: "solid",
      isLoading: isRecalculating,
      isVisible: canEdit && (isEditMode || hasChanges),
    },
    {
      id: "cancel",
      icon: <X size={14} />,
      label: "Anuluj",
      tooltip: "Odrzuć niezapisane zmiany i wyjdź z edycji",
      onClick: onCancelEdit,
      colorScheme: "gray",
      variant: "ghost",
      isVisible: canEdit && isEditMode,
    },
  ];

  const akcjeActions: ActionDef[] = [
    {
      id: "schedule",
      icon: <CalendarDays size={14} />,
      label: hasSchedule ? "Harmonogram" : "Utwórz harmonogram",
      tooltip: hasSchedule
        ? "Przejdź do powiązanego harmonogramu"
        : "Utwórz harmonogram na podstawie kosztorysu",
      onClick: hasSchedule ? onNavigateToSchedule : onCreateSchedule,
      colorScheme: "orange",
      variant: "outline",
      isVisible: canEdit,
    },
    {
      id: "sync",
      icon: <RefreshCw size={14} />,
      label: "Synchronizuj",
      tooltip: "Synchronizuj harmonogram ze strukturą kosztorysu",
      onClick: onSyncSchedule,
      isLoading: isSyncing,
      colorScheme: "gray",
      variant: "ghost",
      isVisible: hasSchedule && canEdit,
    },
    {
      id: "share",
      icon: <Share2 size={14} />,
      label: "Udostępnij",
      tooltip: "Zarządzaj dostępem do kosztorysu",
      onClick: onShare,
      colorScheme: "teal",
      variant: "outline",
      isVisible: canShare,
    },
  ];

  const hasActiveEdit = isEditMode || hasChanges;
  const hasActiveView = !isEditMode && canEdit;
  const hasActiveAction = false;

  // ─── Helpery renderowania ─────────────────────────────────────────────────

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
        color={a.isActive ? "action.600" : undefined}
      >
        {a.label}
      </MenuItem>
    );
  };

  // ─── Layouty ─────────────────────────────────────────────────────────────

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

      {/* ── COMPACT (600–1099px): ikony, akcje → dropdown ── */}
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
                  <MenuButton
                    as={IconButton}
                    icon={<MoreHorizontal size={14} />}
                    size="sm"
                    variant="outline"
                    colorScheme="gray"
                    aria-label="Akcje"
                  />
                  <MenuList minW="200px">
                    {visibleAkcje.map(renderMenuItem)}
                  </MenuList>
                </Menu>
              </Box>
            </>
          )}
        </HStack>
      )}

      {/* ── MOBILE (<600px): 3 grupy jako dropdown ── */}
      {bp === "mobile" && (
        <HStack spacing={2} flexWrap="wrap">
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

          {visibleAkcje.length > 0 && (
            <Box position="relative" display="inline-flex">
              <Menu>
                <MenuButton
                  as={Button}
                  rightIcon={<ChevronDown size={12} />}
                  leftIcon={<Zap size={13} />}
                  size="xs"
                  colorScheme="gray"
                  variant="outline"
                >
                  Akcje
                </MenuButton>
                <MenuList minW="200px">
                  {visibleAkcje.map(renderMenuItem)}
                </MenuList>
              </Menu>
            </Box>
          )}
        </HStack>
      )}
    </Box>
  );
}
