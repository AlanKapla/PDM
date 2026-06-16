import React, { useEffect, useRef, useState } from "react";
import {
  Box,
  Button,
  ButtonGroup,
  Flex,
  HStack,
  IconButton,
  Menu,
  MenuButton,
  MenuDivider,
  MenuItem,
  MenuList,
  Spinner,
  Text,
  Tooltip,
  VStack,
} from "@chakra-ui/react";
import {
  ArrowRight,
  CalendarDays,
  ChevronDown,
  ChevronsDown,
  ChevronsUp,
  LayoutGrid,
  List,
  RefreshCw,
  Settings,
  Share2,
} from "lucide-react";
import { SearchInput } from "./CostEstimate/TreeView/TreeViewHeader";
import type { CostEstimateViewMode } from "./CostEstimate/CostEstimateModernView";

type ToolbarBp = "full" | "compact" | "mobile";

export interface CostEstimateToolbarProps {
  viewMode: CostEstimateViewMode;
  onViewModeChange: (mode: CostEstimateViewMode) => void;
  searchQuery: string;
  onSearchChange: (query: string) => void;
  columnVisibility: React.ReactNode;

  canEdit: boolean;
  canShare: boolean;
  canSchedule: boolean;
  hasSchedule: boolean;
  isSyncing: boolean;
  isRecalculating: boolean;

  onExpandAll: () => void;
  onCollapseAll: () => void;
  onOpenSchema: () => void;
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
  colorScheme?: string;
  variant?: string;
  isLoading?: boolean;
  isVisible: boolean;
}

function ViewModeToggle({
  viewMode,
  onViewModeChange,
  compact,
}: {
  viewMode: CostEstimateViewMode;
  onViewModeChange: (mode: CostEstimateViewMode) => void;
  compact: boolean;
}): React.ReactElement {
  if (compact) {
    return (
      <ButtonGroup size="sm" isAttached variant="outline">
        <Tooltip label="Widok drzewa" hasArrow placement="bottom">
          <IconButton
            aria-label="Drzewo"
            icon={<List size={14} />}
            colorScheme={viewMode === "tree" ? "primary" : "gray"}
            variant={viewMode === "tree" ? "solid" : "outline"}
            onClick={() => onViewModeChange("tree")}
          />
        </Tooltip>
        <Tooltip label="Widok kart" hasArrow placement="bottom">
          <IconButton
            aria-label="Karty"
            icon={<LayoutGrid size={14} />}
            colorScheme={viewMode === "card" ? "primary" : "gray"}
            variant={viewMode === "card" ? "solid" : "outline"}
            onClick={() => onViewModeChange("card")}
          />
        </Tooltip>
      </ButtonGroup>
    );
  }

  return (
    <ButtonGroup
      size="sm"
      isAttached
      variant="outline"
      bg="neutral.50"
      borderRadius="11px"
      p="2px"
      border="1px solid"
      borderColor="neutral.200"
    >
      <Button
        leftIcon={<List size={14} />}
        colorScheme={viewMode === "tree" ? "primary" : "gray"}
        variant={viewMode === "tree" ? "solid" : "ghost"}
        bg={viewMode === "tree" ? "white" : "transparent"}
        boxShadow={viewMode === "tree" ? "0 1px 2px rgba(20,33,47,.05)" : "none"}
        fontWeight="semibold"
        fontSize="sm"
        borderRadius="8px"
        onClick={() => onViewModeChange("tree")}
        _hover={{ bg: viewMode === "tree" ? "white" : "neutral.100" }}
      >
        Drzewo
      </Button>
      <Button
        leftIcon={<LayoutGrid size={14} />}
        colorScheme={viewMode === "card" ? "primary" : "gray"}
        variant={viewMode === "card" ? "solid" : "ghost"}
        bg={viewMode === "card" ? "white" : "transparent"}
        boxShadow={viewMode === "card" ? "0 1px 2px rgba(20,33,47,.05)" : "none"}
        fontWeight="semibold"
        fontSize="sm"
        borderRadius="8px"
        onClick={() => onViewModeChange("card")}
        _hover={{ bg: viewMode === "card" ? "white" : "neutral.100" }}
      >
        Karty
      </Button>
    </ButtonGroup>
  );
}

function renderFullButton(a: ActionDef): React.ReactNode {
  if (!a.isVisible) {
    return null;
  }
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
}

function renderCompactButton(a: ActionDef): React.ReactNode {
  if (!a.isVisible) {
    return null;
  }
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
}

function ScheduleDropdown({
  compact,
  canSchedule,
  hasSchedule,
  isSyncing,
  onNavigateToSchedule,
  onCreateSchedule,
  onSyncSchedule,
}: {
  compact: boolean;
  canSchedule: boolean;
  hasSchedule: boolean;
  isSyncing: boolean;
  onNavigateToSchedule: () => void;
  onCreateSchedule: () => void;
  onSyncSchedule: () => void;
}): React.ReactNode {
  if (!canSchedule) {
    return null;
  }

  const menuList = (
    <MenuList minW="200px">
      {hasSchedule ? (
        <>
          <MenuItem icon={<ArrowRight size={14} />} onClick={onNavigateToSchedule}>
            Przejdź do harmonogramu
          </MenuItem>
          <MenuDivider />
          <MenuItem
            icon={isSyncing ? <Spinner size="xs" /> : <RefreshCw size={14} />}
            onClick={onSyncSchedule}
            isDisabled={isSyncing}
          >
            {isSyncing ? "Synchronizuję…" : "Synchronizuj"}
          </MenuItem>
        </>
      ) : (
        <MenuItem icon={<CalendarDays size={14} />} onClick={onCreateSchedule}>
          Utwórz harmonogram
        </MenuItem>
      )}
    </MenuList>
  );

  if (compact) {
    return (
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
        {menuList}
      </Menu>
    );
  }

  return (
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
      {menuList}
    </Menu>
  );
}

export default function CostEstimateToolbar({
  viewMode,
  onViewModeChange,
  searchQuery,
  onSearchChange,
  columnVisibility,
  canEdit,
  canShare,
  canSchedule,
  hasSchedule,
  isSyncing,
  isRecalculating,
  onExpandAll,
  onCollapseAll,
  onOpenSchema,
  onRefresh,
  onNavigateToSchedule,
  onCreateSchedule,
  onSyncSchedule,
  onShare,
}: CostEstimateToolbarProps): React.ReactElement {
  const containerRef = useRef<HTMLDivElement>(null);
  const [bp, setBp] = useState<ToolbarBp>("full");

  useEffect(() => {
    const el = containerRef.current;
    if (!el) {
      return;
    }

    const measure = (w: number): void => {
      setBp(w >= 900 ? "full" : w >= 520 ? "compact" : "mobile");
    };

    measure(el.offsetWidth);

    const obs = new ResizeObserver(([entry]) => {
      measure(entry.contentRect.width);
    });
    obs.observe(el);
    return () => {
      obs.disconnect();
    };
  }, []);

  const documentActions: ActionDef[] = [
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
      colorScheme: "gray",
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

  const visibleDocument = documentActions.filter((a) => a.isVisible);
  const visibleExpand = expandActions.filter((a) => a.isVisible);
  const useCompactButtons = bp !== "full";
  const renderDocButton = useCompactButtons ? renderCompactButton : renderFullButton;
  const renderExpandButton = useCompactButtons ? renderCompactButton : renderFullButton;

  const documentRow = (
    <HStack spacing={2} flexWrap="wrap" align="center">
      {canEdit && (
        useCompactButtons ? (
          <Tooltip label="Pola dodatkowe" hasArrow placement="bottom">
            <IconButton
              aria-label="Pola dodatkowe"
              icon={<Settings size={14} />}
              size="sm"
              variant="outline"
              colorScheme="gray"
              onClick={onOpenSchema}
            />
          </Tooltip>
        ) : (
          <Button
            leftIcon={<Settings size={14} aria-hidden="true" />}
            size="sm"
            variant="outline"
            colorScheme="gray"
            onClick={onOpenSchema}
          >
            Pola dodatkowe
          </Button>
        )
      )}
      {visibleDocument.map(renderDocButton)}
      <ScheduleDropdown
        compact={useCompactButtons}
        canSchedule={canSchedule}
        hasSchedule={hasSchedule}
        isSyncing={isSyncing}
        onNavigateToSchedule={onNavigateToSchedule}
        onCreateSchedule={onCreateSchedule}
        onSyncSchedule={onSyncSchedule}
      />
    </HStack>
  );

  const viewRow = (
    <HStack spacing={2} flexWrap="wrap" align="center">
      <Box display={{ base: "none", md: "block" }}>
        <ViewModeToggle
          viewMode={viewMode}
          onViewModeChange={onViewModeChange}
          compact={useCompactButtons}
        />
      </Box>
      <SearchInput value={searchQuery} onChange={onSearchChange} />
      {searchQuery && (
        <Text fontSize="12px" color="neutral.400" whiteSpace="nowrap">
          Filtrowanie aktywne
        </Text>
      )}
      <Box display={{ base: "none", md: "block" }}>
        {columnVisibility}
      </Box>
      {visibleExpand.map(renderExpandButton)}
    </HStack>
  );

  if (bp === "mobile") {
    return (
      <Box ref={containerRef} w="100%">
        <VStack align="stretch" spacing={2}>
          {documentRow}
          {viewRow}
        </VStack>
      </Box>
    );
  }

  return (
    <Box ref={containerRef} w="100%">
      <VStack align="stretch" spacing={2}>
        {documentRow}
        {viewRow}
      </VStack>
    </Box>
  );
}
