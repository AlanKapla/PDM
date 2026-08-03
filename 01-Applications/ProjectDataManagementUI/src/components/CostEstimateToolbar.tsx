import React, { useEffect, useRef, useState } from "react";
import {
  Box,
  Button,
  HStack,
  Menu,
  MenuButton,
  MenuDivider,
  MenuItem,
  MenuList,
  Spinner,
} from "@chakra-ui/react";
import {
  ArrowRight,
  CalendarDays,
  ChevronDown,
  ChevronsDown,
  ChevronsUp,
  Eye,
  FileSpreadsheet,
  FileText,
  LayoutGrid,
  List,
  Maximize2,
  Minimize2,
  RefreshCw,
  Settings,
  Share2,
  Zap,
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
  isExportingXlsx: boolean;
  isExportingPdf: boolean;

  onExpandAll: () => void;
  onCollapseAll: () => void;
  onOpenSchema: () => void;
  onRefresh: () => void;
  onNavigateToSchedule: () => void;
  onCreateSchedule: () => void;
  onSyncSchedule: () => void;
  onShare: () => void;
  onExportXlsx: () => void;
  onExportPdf: () => void;
  isFullscreen: boolean;
  onToggleFullscreen: () => void;
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
  isExportingXlsx,
  isExportingPdf,
  onExpandAll,
  onCollapseAll,
  onOpenSchema,
  onRefresh,
  onNavigateToSchedule,
  onCreateSchedule,
  onSyncSchedule,
  onShare,
  onExportXlsx,
  onExportPdf,
  isFullscreen,
  onToggleFullscreen,
}: CostEstimateToolbarProps): React.ReactElement {
  const containerRef = useRef<HTMLDivElement>(null);
  const [bp, setBp] = useState<ToolbarBp>("full");

  useEffect(() => {
    const el = containerRef.current;
    if (!el) {
      return;
    }

    const measure = (w: number): void => {
      setBp(w >= 960 ? "full" : w >= 600 ? "compact" : "mobile");
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

  const isMobile = bp === "mobile";
  const buttonSize = "sm";

  return (
    <Box ref={containerRef} w="100%">
      <HStack spacing={2} flexWrap="wrap" align="center">
        <Menu>
          <MenuButton
            as={Button}
            size={buttonSize}
            variant="outline"
            colorScheme="gray"
            leftIcon={<Zap size={16} aria-hidden="true" />}
            rightIcon={<ChevronDown size={12} aria-hidden="true" />}
          >
            Akcje
          </MenuButton>
          <MenuList minW="200px">
            {canSchedule && hasSchedule && (
              <>
                <MenuItem icon={<ArrowRight size={16} />} onClick={onNavigateToSchedule}>
                  Przejdź do harmonogramu
                </MenuItem>
                <MenuItem
                  icon={isSyncing ? <Spinner size="xs" /> : <RefreshCw size={16} />}
                  onClick={onSyncSchedule}
                  isDisabled={isSyncing}
                >
                  {isSyncing ? "Synchronizuję…" : "Synchronizuj harmonogram"}
                </MenuItem>
                <MenuDivider />
              </>
            )}
            {canSchedule && !hasSchedule && (
              <MenuItem icon={<CalendarDays size={16} />} onClick={onCreateSchedule}>
                Utwórz harmonogram
              </MenuItem>
            )}
            <MenuItem
              icon={isRecalculating ? <Spinner size="xs" /> : <RefreshCw size={16} aria-hidden="true" />}
              onClick={onRefresh}
              isDisabled={isRecalculating || isExportingXlsx || isExportingPdf}
            >
              {isRecalculating ? "Odświeżam…" : "Odśwież"}
            </MenuItem>
            <MenuItem
              icon={
                isExportingXlsx ? (
                  <Spinner size="xs" />
                ) : (
                  <FileSpreadsheet size={16} aria-hidden="true" />
                )
              }
              onClick={onExportXlsx}
              isDisabled={isExportingXlsx || isExportingPdf}
              aria-busy={isExportingXlsx}
            >
              {isExportingXlsx ? "Eksportuję…" : "Eksportuj do Excel"}
            </MenuItem>
            <MenuItem
              icon={
                isExportingPdf ? (
                  <Spinner size="xs" />
                ) : (
                  <FileText size={16} aria-hidden="true" />
                )
              }
              onClick={onExportPdf}
              isDisabled={isExportingXlsx || isExportingPdf}
              aria-busy={isExportingPdf}
            >
              {isExportingPdf ? "Eksportuję…" : "Eksportuj do PDF"}
            </MenuItem>
            {canShare && (
              <MenuItem icon={<Share2 size={16} aria-hidden="true" />} onClick={onShare}>
                Udostępnij
              </MenuItem>
            )}
            {canEdit && (
              <MenuItem icon={<Settings size={16} aria-hidden="true" />} onClick={onOpenSchema}>
                Pola dodatkowe
              </MenuItem>
            )}
          </MenuList>
        </Menu>

        <Menu>
          <MenuButton
            as={Button}
            size={buttonSize}
            variant="outline"
            colorScheme="gray"
            leftIcon={<Eye size={16} aria-hidden="true" />}
            rightIcon={<ChevronDown size={12} aria-hidden="true" />}
          >
            Widok
          </MenuButton>
          <MenuList minW="200px">
            <MenuItem icon={<ChevronsDown size={16} />} onClick={onExpandAll}>
              Rozwiń wszystko
            </MenuItem>
            <MenuItem icon={<ChevronsUp size={16} />} onClick={onCollapseAll}>
              Zwiń wszystko
            </MenuItem>
            {!isMobile && (
              <>
                <MenuDivider />
                <MenuItem
                  icon={<List size={16} />}
                  onClick={() => onViewModeChange("tree")}
                  fontWeight={viewMode === "tree" ? "semibold" : "normal"}
                  color={viewMode === "tree" ? "action.600" : undefined}
                >
                  Drzewo
                </MenuItem>
                <MenuItem
                  icon={<LayoutGrid size={16} />}
                  onClick={() => onViewModeChange("card")}
                  fontWeight={viewMode === "card" ? "semibold" : "normal"}
                  color={viewMode === "card" ? "action.600" : undefined}
                >
                  Karty
                </MenuItem>
              </>
            )}
            <MenuDivider />
            <MenuItem
              icon={isFullscreen ? <Minimize2 size={16} /> : <Maximize2 size={16} />}
              onClick={onToggleFullscreen}
            >
              {isFullscreen ? "Zamknij pełny ekran" : "Pełny ekran"}
            </MenuItem>
          </MenuList>
        </Menu>

        {!isMobile && viewMode === "tree" && columnVisibility}

        <SearchInput value={searchQuery} onChange={onSearchChange} />
      </HStack>
    </Box>
  );
}
