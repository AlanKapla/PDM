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
  isDirty: boolean;
  isSyncing: boolean;
  canEdit: boolean;

  // Handlers
  onNavigateToCostEstimate: () => void;
  onToggleComments: () => void;
  onSyncFromCostEstimate: () => void;
  onAddStage: () => void;
  onSave: () => void;
  onCancel: () => void;
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
      bg="action.400"
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
  isDirty,
  isSyncing,
  canEdit,
  onNavigateToCostEstimate,
  onToggleComments,
  onSyncFromCostEstimate,
  onAddStage,
  onSave,
  onCancel,
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
      setBp(w >= 960 ? "full" : w >= 600 ? "compact" : "mobile");

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
      icon: hideWeekends ? <Eye size={16} /> : <EyeOff size={16} />,
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
      icon: <CalendarDays size={16} />,
      label: "Dzisiaj",
      tooltip: "Przewiń do dzisiejszej daty",
      onClick: onScrollToToday,
      colorScheme: "primary",
      variant: "outline",
      isVisible: true,
    },
    {
      id: "expand",
      icon: <ChevronDown size={16} />,
      label: "Rozwiń",
      tooltip: "Rozwiń wszystkie etapy",
      onClick: onExpandAll,
      colorScheme: "gray",
      variant: "outline",
      isVisible: true,
    },
    {
      id: "collapse",
      icon: <ChevronUp size={16} />,
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
      id: "add-stage",
      icon: <Plus size={16} />,
      label: "Dodaj etap",
      tooltip: "Dodaj nowy etap do harmonogramu",
      onClick: onAddStage,
      colorScheme: "gray",
      variant: "outline",
      isVisible: canEdit,
    },
    {
      id: "save",
      icon: <Save size={16} />,
      label: "Zapisz",
      tooltip: "Zapisz zmiany",
      onClick: onSave,
      colorScheme: "primary",
      variant: "solid",
      isVisible: canEdit && isDirty,
    },
    {
      id: "cancel",
      icon: <X size={16} />,
      label: "Anuluj",
      tooltip: "Odrzuć niezapisane zmiany",
      onClick: onCancel,
      colorScheme: "gray",
      variant: "ghost",
      isVisible: canEdit && isDirty,
    },
  ];

  const akcjeActions: ActionDef[] = [
    {
      id: "cost-estimate",
      icon: <FileSpreadsheet size={16} />,
      label: "Kosztorys",
      tooltip: "Przejdź do powiązanego kosztorysu",
      onClick: onNavigateToCostEstimate,
      colorScheme: "orange",
      variant: "ghost",
      isVisible: hasCostEstimate,
    },
    {
      id: "comments",
      icon: <MessageSquare size={16} />,
      label: "Komentarze",
      tooltip: showComments ? "Ukryj komentarze do prac" : "Pokaż komentarze do prac",
      onClick: onToggleComments,
      isActive: showComments,
      colorScheme: showComments ? "primary" : "gray",
      variant: showComments ? "solid" : "ghost",
      isVisible: true,
    },
    {
      id: "sync",
      icon: <RefreshCw size={16} />,
      label: "Odśwież z kosztorysu",
      tooltip: "Aktualizuje strukturę etapów na podstawie grup w kosztorysie",
      onClick: onSyncFromCostEstimate,
      isLoading: isSyncing,
      colorScheme: "gray",
      variant: "ghost",
      isVisible: hasCostEstimate && canEdit,
    },
  ];

  // Aktywne stany grup (dla wskaźnika w trybie zwinięcia)
  const hasActiveEdit = isDirty;
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
        color={a.isActive ? "action.600" : undefined}
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
                      icon={<MoreHorizontal size={16} />}
                      size="sm"
                      variant="outline"
                      colorScheme={hasActiveAction ? "primary" : "gray"}
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

      {/* ── MOBILE (<600px): grupy jako osobne przyciski z dropdown ── */}
      {bp === "mobile" && (
        <HStack spacing={2} flexWrap="wrap">
          {/* Widok */}
          {visibleView.length > 0 && (
            <Box position="relative" display="inline-flex">
              <Menu>
                <MenuButton
                  as={Button}
                  rightIcon={<ChevronDown size={16} />}
                  leftIcon={<Eye size={16} />}
                  size="xs"
                  colorScheme={hasActiveView ? "primary" : "gray"}
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

          {/* Edycja (dodawanie/zapisywanie) */}
          {visibleEdit.length > 0 && (
            <Box position="relative" display="inline-flex">
              <Menu>
                <MenuButton
                  as={Button}
                  rightIcon={<ChevronDown size={16} />}
                  leftIcon={<Edit size={16} />}
                  size="xs"
                  colorScheme={hasActiveEdit ? "primary" : "gray"}
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
                  rightIcon={<ChevronDown size={16} />}
                  leftIcon={<Zap size={16} />}
                  size="xs"
                  colorScheme={hasActiveAction ? "primary" : "gray"}
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
