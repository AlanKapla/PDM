import React, { useEffect, useRef, useState } from "react";
import {
  Box,
  Button,
  Divider,
  Flex,
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
  ArrowRight,
  CalendarDays,
  ChevronDown,
  ChevronsDown,
  ChevronsUp,
  Edit,
  Eye,
  RefreshCw,
  Share2,
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
  onRefresh: () => void;
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
  onRefresh,
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

  const modeActions: ActionDef[] = [
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
  ];

  const otherActions: ActionDef[] = [
    {
      id: "refresh",
      icon: <RefreshCw size={14} />,
      label: "Odśwież",
      tooltip: "Przelicz i odśwież dane kosztorysu",
      onClick: onRefresh,
      isLoading: isRecalculating,
      colorScheme: "gray",
      variant: "outline",
      isVisible: true,
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

  const expandActions: ActionDef[] = [
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
  ];

  const hasActiveEdit = isEditMode || hasChanges;

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

  // ─── Dropdown Harmonogram ─────────────────────────────────────────────────

  const scheduleDropdownFull = canEdit ? (
    <Menu>
      <Tooltip label="Operacje na powiązanym harmonogramie" hasArrow placement="bottom" openDelay={400}>
        <MenuButton
          as={Button}
          size="sm"
          leftIcon={<CalendarDays size={14} />}
          rightIcon={<ChevronDown size={12} />}
          colorScheme="orange"
          variant="outline"
        >
          Harmonogram
        </MenuButton>
      </Tooltip>
      <MenuList minW="200px">
        {hasSchedule ? (
          <>
            <MenuItem icon={<ArrowRight size={14} />} onClick={onNavigateToSchedule}>
              Przejdź do harmonogramu
            </MenuItem>
            <MenuDivider />
            <MenuItem icon={<RefreshCw size={14} />} onClick={onSyncSchedule}>
              {isSyncing ? "Synchronizuję…" : "Synchronizuj"}
            </MenuItem>
          </>
        ) : (
          <MenuItem icon={<CalendarDays size={14} />} onClick={onCreateSchedule}>
            Utwórz harmonogram
          </MenuItem>
        )}
      </MenuList>
    </Menu>
  ) : null;

  const scheduleDropdownCompact = canEdit ? (
    <Menu>
      <Tooltip label="Harmonogram" hasArrow placement="bottom">
        <MenuButton
          as={IconButton}
          icon={<CalendarDays size={14} />}
          size="sm"
          colorScheme="orange"
          variant="outline"
          aria-label="Harmonogram"
        />
      </Tooltip>
      <MenuList minW="200px">
        {hasSchedule ? (
          <>
            <MenuItem icon={<ArrowRight size={14} />} onClick={onNavigateToSchedule}>
              Przejdź do harmonogramu
            </MenuItem>
            <MenuDivider />
            <MenuItem icon={<RefreshCw size={14} />} onClick={onSyncSchedule}>
              {isSyncing ? "Synchronizuję…" : "Synchronizuj"}
            </MenuItem>
          </>
        ) : (
          <MenuItem icon={<CalendarDays size={14} />} onClick={onCreateSchedule}>
            Utwórz harmonogram
          </MenuItem>
        )}
      </MenuList>
    </Menu>
  ) : null;

  // ─── Layouty ─────────────────────────────────────────────────────────────

  const visibleMode = modeActions.filter((a) => a.isVisible);
  const visibleOther = otherActions.filter((a) => a.isVisible);
  const visibleExpand = expandActions.filter((a) => a.isVisible);

  const hasModeSection = visibleMode.length > 0;

  return (
    <Box ref={containerRef} w="100%">
      {/* ── FULL (≥1100px): przyciski z etykietami, expand po prawej ── */}
      {bp === "full" && (
        <Flex justify="space-between" align="center" gap={2}>
          {/* Lewa strona: tryb + harmonogram + odśwież + udostępnij */}
          <HStack spacing={2} flexWrap="wrap">
            {visibleMode.map(renderFullButton)}
            {hasModeSection && scheduleDropdownFull && (
              <Divider orientation="vertical" height="20px" alignSelf="center" />
            )}
            {scheduleDropdownFull}
            {(hasModeSection || scheduleDropdownFull) && visibleOther.length > 0 && (
              <Divider orientation="vertical" height="20px" alignSelf="center" />
            )}
            {visibleOther.map(renderFullButton)}
          </HStack>

          {/* Prawa strona: rozwiń / zwiń */}
          <HStack spacing={2} flexShrink={0}>
            {visibleExpand.map(renderFullButton)}
          </HStack>
        </Flex>
      )}

      {/* ── COMPACT (600–1099px): ikony, expand po prawej ── */}
      {bp === "compact" && (
        <Flex justify="space-between" align="center" gap={2}>
          {/* Lewa strona */}
          <HStack spacing={2} flexWrap="wrap">
            {visibleMode.map(renderCompactButton)}
            {hasModeSection && scheduleDropdownCompact && (
              <Divider orientation="vertical" height="20px" alignSelf="center" />
            )}
            {scheduleDropdownCompact}
            {(hasModeSection || scheduleDropdownCompact) && visibleOther.length > 0 && (
              <Divider orientation="vertical" height="20px" alignSelf="center" />
            )}
            {visibleOther.map(renderCompactButton)}
          </HStack>

          {/* Prawa strona: rozwiń / zwiń */}
          <HStack spacing={2} flexShrink={0}>
            {visibleExpand.map(renderCompactButton)}
          </HStack>
        </Flex>
      )}

      {/* ── MOBILE (<600px): skonsolidowane dropdown ── */}
      {bp === "mobile" && (
        <HStack spacing={2} flexWrap="wrap">
          {visibleMode.length > 0 && (
            <Box position="relative" display="inline-flex">
              <Menu>
                <MenuButton
                  as={Button}
                  rightIcon={<ChevronDown size={12} />}
                  leftIcon={<Eye size={13} />}
                  size="xs"
                  colorScheme={hasActiveEdit ? "green" : "gray"}
                  variant={hasActiveEdit ? "solid" : "outline"}
                >
                  Tryb
                </MenuButton>
                <MenuList minW="200px">
                  {visibleMode.map(renderMenuItem)}
                </MenuList>
              </Menu>
              {hasActiveEdit && <ActiveDot />}
            </Box>
          )}

          {canEdit && (
            <Box position="relative" display="inline-flex">
              <Menu>
                <MenuButton
                  as={Button}
                  rightIcon={<ChevronDown size={12} />}
                  leftIcon={<CalendarDays size={13} />}
                  size="xs"
                  colorScheme="orange"
                  variant="outline"
                >
                  Harmonogram
                </MenuButton>
                <MenuList minW="200px">
                  {hasSchedule ? (
                    <>
                      <MenuItem icon={<ArrowRight size={14} />} onClick={onNavigateToSchedule}>
                        Przejdź do harmonogramu
                      </MenuItem>
                      <MenuDivider />
                      <MenuItem icon={<RefreshCw size={14} />} onClick={onSyncSchedule}>
                        Synchronizuj
                      </MenuItem>
                    </>
                  ) : (
                    <MenuItem icon={<CalendarDays size={14} />} onClick={onCreateSchedule}>
                      Utwórz harmonogram
                    </MenuItem>
                  )}
                </MenuList>
              </Menu>
            </Box>
          )}

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
                Więcej
              </MenuButton>
              <MenuList minW="200px">
                {visibleOther.map(renderMenuItem)}
                <MenuDivider />
                {visibleExpand.map(renderMenuItem)}
              </MenuList>
            </Menu>
          </Box>
        </HStack>
      )}
    </Box>
  );
}
