import React, { useState, useRef } from "react";
import {
  Box,
  HStack,
  Text,
  IconButton,
  Button,
  Input,
  Tooltip,
  Menu,
  MenuButton,
  MenuList,
  MenuItem,
  MenuDivider,
  useColorModeValue,
  Spinner,
} from "@chakra-ui/react";
import {
  ArrowLeft,
  ChevronDown,
  ChevronRight,
  RefreshCw,
  CalendarDays,
  ChevronsDown,
  ChevronsUp,
  GitBranch,
  Eye,
  Zap,
  Maximize2,
  Minimize2,
} from "lucide-react";
import { useNavigate } from "react-router-dom";
import { useGantt } from "./GanttContext";
import { SearchInput } from "../CostEstimate/TreeView/TreeViewHeader";
import type { TimeScale } from "../../hooks/useTimelineData";

/** MenuList portaled do body ma domyślny z-index 1 — musi być nad timeline (do ~300). */
const GANTT_MENU_Z_INDEX = 1500;

interface GanttToolbarProps {
  onNavigateBack: () => void;
  timeScale: TimeScale;
  onTimeScaleChange: (s: TimeScale) => void;
  onScrollToToday: () => void;
  hideWeekends: boolean;
  onToggleWeekends: () => void;
  searchQuery: string;
  onSearchChange: (query: string) => void;
  /** W trybie kompaktowym (np. modal) — zachowany dla kompatybilności API */
  compact?: boolean;
  isFullscreen: boolean;
  onToggleFullscreen: () => void;
}

export default function GanttToolbar({
  onNavigateBack,
  timeScale,
  onTimeScaleChange,
  onScrollToToday,
  searchQuery,
  onSearchChange,
  isFullscreen,
  onToggleFullscreen,
}: GanttToolbarProps): React.ReactElement {
  const navigate = useNavigate();
  const {
    schedule,
    canEdit,
    expandAll,
    collapseAll,
    fetchSchedule,
    isLoading,
    isMutating,
    renameSchedule,
    showDependencies,
    setShowDependencies,
    syncWithEstimate,
    projectId,
  } = useGantt();

  const isSyncing = isMutating.has("syncWithEstimate");
  const handleNavigateToCostEstimate = (): void => {
    navigate(`/projects/${projectId}/cost-estimates/${schedule?.costEstimateId}`);
  };

  const [isEditingName, setIsEditingName] = useState(false);
  const [nameInput, setNameInput] = useState(schedule?.name ?? "");
  const nameInputRef = useRef<HTMLInputElement>(null);

  const borderColor = useColorModeValue("gray.200", "gray.700");
  const bg = useColorModeValue("white", "gray.800");

  const handleNameClick = (): void => {
    if (!canEdit) {
      return;
    }
    setNameInput(schedule?.name ?? "");
    setIsEditingName(true);
    setTimeout(() => nameInputRef.current?.focus(), 50);
  };

  const handleNameBlur = async (): Promise<void> => {
    setIsEditingName(false);
    const trimmed = nameInput.trim();
    if (!trimmed || trimmed === schedule?.name) {
      return;
    }
    await renameSchedule(trimmed);
  };

  const handleNameKeyDown = (e: React.KeyboardEvent): void => {
    if (e.key === "Enter") {
      (e.target as HTMLInputElement).blur();
    }
    if (e.key === "Escape") {
      setIsEditingName(false);
      setNameInput(schedule?.name ?? "");
    }
  };

  const isRenamingSchedule = isMutating.has("renameSchedule");
  const hasCostEstimate = !!schedule?.costEstimateId;

  const buttonSize = "sm";

  return (
    <Box
      bg={bg}
      borderBottomWidth="1px"
      borderColor={borderColor}
      px={5}
      py={3}
      position="sticky"
      top={0}
      zIndex={30}
      shadow="sm"
    >
      <HStack spacing={3} mb={2} flexWrap="wrap">
        <Tooltip label="Wróć do listy harmonogramów" hasArrow placement="bottom">
          <IconButton
            aria-label="Wróć"
            icon={<ArrowLeft size={16} />}
            size="sm"
            variant="ghost"
            onClick={onNavigateBack}
          />
        </Tooltip>

        <HStack spacing={2} flex={1} minW={0}>
          {isEditingName ? (
            <Input
              ref={nameInputRef}
              value={nameInput}
              onChange={(e) => setNameInput(e.target.value)}
              onBlur={handleNameBlur}
              onKeyDown={handleNameKeyDown}
              size="sm"
              fontWeight="semibold"
              fontSize="lg"
              maxW="400px"
              isDisabled={isRenamingSchedule}
            />
          ) : (
            <Text
              fontWeight="semibold"
              fontSize="lg"
              noOfLines={1}
              cursor={canEdit ? "pointer" : "default"}
              onClick={handleNameClick}
              title={canEdit ? "Kliknij aby zmienić nazwę" : schedule?.name}
              _hover={canEdit ? { textDecoration: "underline", textDecorationStyle: "dotted" } : {}}
            >
              {schedule?.name}
            </Text>
          )}
          {isRenamingSchedule && <Spinner size="xs" />}
        </HStack>
      </HStack>

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
          <MenuList zIndex={GANTT_MENU_Z_INDEX} minW="200px">
            <MenuItem
              icon={isLoading ? <Spinner size="xs" /> : <RefreshCw size={16} />}
              onClick={fetchSchedule}
              isDisabled={isLoading}
            >
              {isLoading ? "Odświeżam…" : "Odśwież"}
            </MenuItem>
            {hasCostEstimate && (
              <MenuItem
                icon={isSyncing ? <Spinner size="xs" /> : <RefreshCw size={16} />}
                onClick={syncWithEstimate}
                isDisabled={isSyncing}
              >
                {isSyncing ? "Synchronizuję…" : "Synchronizuj z kosztorysem"}
              </MenuItem>
            )}
            {hasCostEstimate && (
              <MenuItem icon={<ChevronRight size={16} />} onClick={handleNavigateToCostEstimate}>
                Przejdź do kosztorysu
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
          <MenuList zIndex={GANTT_MENU_Z_INDEX} minW="200px">
            <MenuItem
              icon={<GitBranch size={16} />}
              onClick={() => setShowDependencies(!showDependencies)}
              fontWeight={showDependencies ? "semibold" : "normal"}
              color={showDependencies ? "action.600" : undefined}
            >
              {showDependencies ? "Ukryj zależności" : "Pokaż zależności"}
            </MenuItem>
            <MenuItem
              icon={<ChevronsDown size={16} />}
              onClick={() => expandAll(schedule?.stages ?? [])}
            >
              Rozwiń wszystko
            </MenuItem>
            <MenuItem icon={<ChevronsUp size={16} />} onClick={collapseAll}>
              Zwiń wszystko
            </MenuItem>
            <MenuItem icon={<CalendarDays size={16} />} onClick={onScrollToToday}>
              Dziś
            </MenuItem>
            <MenuDivider />
            <MenuItem
              onClick={() => onTimeScaleChange("days")}
              fontWeight={timeScale === "days" ? "semibold" : "normal"}
              color={timeScale === "days" ? "action.600" : undefined}
            >
              Dni
            </MenuItem>
            <MenuItem
              onClick={() => onTimeScaleChange("weeks")}
              fontWeight={timeScale === "weeks" ? "semibold" : "normal"}
              color={timeScale === "weeks" ? "action.600" : undefined}
            >
              Tyg.
            </MenuItem>
            <MenuItem
              onClick={() => onTimeScaleChange("months")}
              fontWeight={timeScale === "months" ? "semibold" : "normal"}
              color={timeScale === "months" ? "action.600" : undefined}
            >
              Mies.
            </MenuItem>
            <MenuDivider />
            <MenuItem
              icon={isFullscreen ? <Minimize2 size={16} /> : <Maximize2 size={16} />}
              onClick={onToggleFullscreen}
            >
              {isFullscreen ? "Zamknij pełny ekran" : "Pełny ekran"}
            </MenuItem>
          </MenuList>
        </Menu>

        <SearchInput
          value={searchQuery}
          onChange={onSearchChange}
          placeholder="Szukaj w harmonogramie..."
          ariaLabel="Szukaj w harmonogramie"
        />
      </HStack>
    </Box>
  );
}
