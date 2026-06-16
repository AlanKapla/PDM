import { useState, useRef, useEffect } from "react";
import {
  Box,
  Flex,
  Divider,
  HStack,
  VStack,
  Text,
  IconButton,
  Button,
  ButtonGroup,
  Badge,
  Input,
  Tooltip,
  Menu,
  MenuButton,
  MenuList,
  MenuItem,
  useColorModeValue,
  Spinner,
} from "@chakra-ui/react";
import {
  ArrowLeft,
  ChevronDown,
  ChevronRight,
  Edit3,
  Eye,
  RefreshCw,
  CalendarDays,
  ChevronsDown,
  ChevronsUp,
  FileSpreadsheet,
  GitBranch,
  CalendarX2,
  Zap,
} from "lucide-react";
import { useNavigate } from "react-router-dom";
import { useGantt } from "./GanttContext";
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
  /** W trybie kompaktowym (np. modal) ukrywa skalę czasu, zależności, weekendy i przycisk Dziś */
  compact?: boolean;
}

export default function GanttToolbar({
  onNavigateBack,
  timeScale,
  onTimeScaleChange,
  onScrollToToday,
  hideWeekends,
  onToggleWeekends,
  compact = false,
}: GanttToolbarProps) {
  const navigate = useNavigate();
  const {
    schedule,
    mode,
    setMode,
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
  const handleNavigateToCostEstimate = () =>
    navigate(`/projects/${projectId}/cost-estimates/${schedule?.costEstimateId}`);

  const [isEditingName, setIsEditingName] = useState(false);
  const [nameInput, setNameInput] = useState(schedule?.name ?? "");
  const nameInputRef = useRef<HTMLInputElement>(null);

  const borderColor = useColorModeValue("gray.200", "gray.700");
  const bg = useColorModeValue("white", "gray.800");
  const activeModeColor = "green.500";

  const handleNameClick = () => {
    if (!canEdit) return;
    setNameInput(schedule?.name ?? "");
    setIsEditingName(true);
    setTimeout(() => nameInputRef.current?.focus(), 50);
  };

  const handleNameBlur = async () => {
    setIsEditingName(false);
    const trimmed = nameInput.trim();
    if (!trimmed || trimmed === schedule?.name) return;
    await renameSchedule(trimmed);
  };

  const handleNameKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Enter") (e.target as HTMLInputElement).blur();
    if (e.key === "Escape") { setIsEditingName(false); setNameInput(schedule?.name ?? ""); }
  };

  const isRenamingSchedule = isMutating.has("renameSchedule");
  const hasCostEstimate = !!schedule?.costEstimateId;

  const containerRef = useRef<HTMLDivElement>(null);
  const [bp, setBp] = useState<"full" | "compact" | "mobile">("full");

  useEffect(() => {
    const el = containerRef.current;
    if (!el) return;
    const measure = (w: number) =>
      setBp(w >= 960 ? "full" : w >= 520 ? "compact" : "mobile");
    measure(el.offsetWidth);
    const obs = new ResizeObserver(([entry]) => measure(entry.contentRect.width));
    obs.observe(el);
    return () => obs.disconnect();
  }, []);

  return (
    <Box
      ref={containerRef}
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
      {/* Wiersz 1: Nawigacja + Nazwa + Status */}
      <HStack spacing={3} mb={2} flexWrap="wrap">
        {/* Powrót */}
        <Tooltip label="Wróć do listy harmonogramów">
          <IconButton
            aria-label="Wróć"
            icon={<ArrowLeft size={18} />}
            size="sm"
            variant="ghost"
            onClick={onNavigateBack}
          />
        </Tooltip>

        {/* Nazwa harmonogramu */}
        <HStack spacing={2} flex={1} minW={0}>
          {isEditingName ? (
            <Input
              ref={nameInputRef}
              value={nameInput}
              onChange={e => setNameInput(e.target.value)}
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

      {/* Wiersz 2: Tryb + Kosztorys + Narzędzia */}

      {/* ── FULL (≥960px): przyciski z etykietami ── */}
      {bp === "full" && (
        <HStack spacing={2} flexWrap="wrap" justify="space-between">
          {/* Lewa: tryb Podgląd / Edycja + Kosztorys + Odśwież */}
          <HStack spacing={1}>
            <ButtonGroup size="sm" variant="outline" isAttached>
              <Button
                leftIcon={<Eye size={14} />}
                colorScheme={mode === "view" ? "primary" : "gray"}
                variant={mode === "view" ? "solid" : "outline"}
                onClick={() => setMode("view")}
              >
                Podgląd
              </Button>
              {canEdit && (
                <Button
                  leftIcon={<Edit3 size={14} />}
                  colorScheme={mode === "edit" ? "primary" : "gray"}
                  variant={mode === "edit" ? "solid" : "outline"}
                  onClick={() => setMode("edit")}
                >
                  Edycja
                </Button>
              )}
            </ButtonGroup>

            {hasCostEstimate && (
              <Menu>
                <MenuButton
                  as={Button}
                  size="sm"
                  variant="outline"
                  colorScheme="orange"
                  leftIcon={<FileSpreadsheet size={14} />}
                  rightIcon={<ChevronDown size={12} />}
                >
                  Kosztorys
                </MenuButton>
                <MenuList zIndex={GANTT_MENU_Z_INDEX}>
                  <MenuItem
                    icon={isSyncing ? <Spinner size="xs" /> : <RefreshCw size={14} />}
                    onClick={syncWithEstimate}
                    isDisabled={isSyncing}
                  >
                    Synchronizuj z kosztorysem
                  </MenuItem>
                  <MenuItem icon={<ChevronRight size={14} />} onClick={handleNavigateToCostEstimate}>
                    Przejdź do kosztorysu
                  </MenuItem>
                </MenuList>
              </Menu>
            )}

            <Button size="sm" variant="outline" leftIcon={<RefreshCw size={14} />} isLoading={isLoading} onClick={fetchSchedule}>
              Odśwież
            </Button>
          </HStack>

          {/* Prawa: Zależności + Weekendy + Expand + Dziś + Skala */}
          <HStack spacing={1}>
            {!compact && (
              <>
                <Button size="sm" variant={showDependencies ? "solid" : "outline"} colorScheme={showDependencies ? "level2" : "gray"} leftIcon={<GitBranch size={14} />} onClick={() => setShowDependencies(!showDependencies)}>
                  Zależności
                </Button>
                <Button size="sm" variant={hideWeekends ? "solid" : "outline"} colorScheme={hideWeekends ? "level2" : "gray"} leftIcon={<CalendarX2 size={14} />} onClick={onToggleWeekends}>
                  Ukryj weekendy
                </Button>
              </>
            )}
            <Button size="sm" variant="outline" leftIcon={<ChevronsDown size={14} />} onClick={() => expandAll(schedule?.stages ?? [])}>
              Rozwiń wszystko
            </Button>
            <Button size="sm" variant="outline" leftIcon={<ChevronsUp size={14} />} onClick={collapseAll}>
              Zwiń wszystko
            </Button>
            {!compact && (
              <>
                <Button size="sm" variant="outline" colorScheme="primary" leftIcon={<CalendarDays size={14} />} onClick={onScrollToToday}>
                  Dziś
                </Button>
                <Box w="1px" h="20px" bg={borderColor} mx={1} />
                <ButtonGroup size="sm" isAttached variant="outline">
                  {(["days", "weeks", "months"] as TimeScale[]).map(s => (
                    <Button key={s} onClick={() => onTimeScaleChange(s)} colorScheme={timeScale === s ? "primary" : "gray"} variant={timeScale === s ? "solid" : "outline"}>
                      {s === "days" ? "Dni" : s === "weeks" ? "Tyg." : "Mies."}
                    </Button>
                  ))}
                </ButtonGroup>
              </>
            )}
          </HStack>
        </HStack>
      )}

      {/* ── COMPACT (520–959px): ikony z tooltipami ── */}
      {bp === "compact" && (
        <Flex justify="space-between" align="center" gap={2}>
          <HStack spacing={1} flexWrap="wrap">
            <ButtonGroup size="sm" isAttached variant="outline">
              <Tooltip label="Tryb podglądu" hasArrow>
                <IconButton aria-label="Podgląd" icon={<Eye size={14} />} size="sm" colorScheme={mode === "view" ? "primary" : "gray"} variant={mode === "view" ? "solid" : "outline"} onClick={() => setMode("view")} />
              </Tooltip>
              {canEdit && (
                <Tooltip label="Tryb edycji" hasArrow>
                  <IconButton aria-label="Edycja" icon={<Edit3 size={14} />} size="sm" colorScheme={mode === "edit" ? "primary" : "gray"} variant={mode === "edit" ? "solid" : "outline"} onClick={() => setMode("edit")} />
                </Tooltip>
              )}
            </ButtonGroup>

            {hasCostEstimate && (
              <Menu>
                <Tooltip label="Kosztorys" hasArrow>
                  <MenuButton as={IconButton} aria-label="Kosztorys" icon={<FileSpreadsheet size={14} />} size="sm" variant="outline" colorScheme="orange" />
                </Tooltip>
                <MenuList zIndex={GANTT_MENU_Z_INDEX}>
                  <MenuItem icon={isSyncing ? <Spinner size="xs" /> : <RefreshCw size={14} />} onClick={syncWithEstimate} isDisabled={isSyncing}>
                    Synchronizuj z kosztorysem
                  </MenuItem>
                  <MenuItem icon={<ChevronRight size={14} />} onClick={handleNavigateToCostEstimate}>
                    Przejdź do kosztorysu
                  </MenuItem>
                </MenuList>
              </Menu>
            )}

            <Tooltip label="Odśwież harmonogram" hasArrow>
              <IconButton aria-label="Odśwież" icon={<RefreshCw size={14} />} size="sm" variant="outline" isLoading={isLoading} onClick={fetchSchedule} />
            </Tooltip>

            {!compact && <Divider orientation="vertical" height="20px" alignSelf="center" />}

            {!compact && (
              <>
                <Tooltip label="Pokaż zależności" hasArrow>
                  <IconButton aria-label="Zależności" icon={<GitBranch size={14} />} size="sm" variant={showDependencies ? "solid" : "outline"} colorScheme={showDependencies ? "level2" : "gray"} onClick={() => setShowDependencies(!showDependencies)} />
                </Tooltip>
                <Tooltip label="Ukryj weekendy" hasArrow>
                  <IconButton aria-label="Ukryj weekendy" icon={<CalendarX2 size={14} />} size="sm" variant={hideWeekends ? "solid" : "outline"} colorScheme={hideWeekends ? "level2" : "gray"} onClick={onToggleWeekends} />
                </Tooltip>
              </>
            )}
            <Tooltip label="Rozwiń wszystko" hasArrow>
              <IconButton aria-label="Rozwiń wszystko" icon={<ChevronsDown size={14} />} size="sm" variant="outline" onClick={() => expandAll(schedule?.stages ?? [])} />
            </Tooltip>
            <Tooltip label="Zwiń wszystko" hasArrow>
              <IconButton aria-label="Zwiń wszystko" icon={<ChevronsUp size={14} />} size="sm" variant="outline" onClick={collapseAll} />
            </Tooltip>
            {!compact && (
              <>
                <Tooltip label="Przewiń do dziś" hasArrow>
                  <IconButton aria-label="Dziś" icon={<CalendarDays size={14} />} size="sm" variant="outline" colorScheme="primary" onClick={onScrollToToday} />
                </Tooltip>

                <Box w="1px" h="20px" bg={borderColor} mx={1} />

                <ButtonGroup size="sm" isAttached variant="outline">
                  {(["days", "weeks", "months"] as TimeScale[]).map(s => (
                    <Button key={s} onClick={() => onTimeScaleChange(s)} colorScheme={timeScale === s ? "primary" : "gray"} variant={timeScale === s ? "solid" : "outline"}>
                      {s === "days" ? "Dni" : s === "weeks" ? "Tyg." : "Mies."}
                    </Button>
                  ))}
                </ButtonGroup>
              </>
            )}
          </HStack>
        </Flex>
      )}

      {/* ── MOBILE (<520px): skonsolidowane dropdowny ── */}
      {bp === "mobile" && (
        <HStack spacing={2} flexWrap="wrap">
          {/* Na mobile zawsze tryb edycji */}

          {hasCostEstimate && (
            <Menu>
              <MenuButton as={Button} size="xs" rightIcon={<ChevronDown size={12} />} leftIcon={<FileSpreadsheet size={13} />} colorScheme="orange" variant="outline">
                Kosztorys
              </MenuButton>
              <MenuList zIndex={GANTT_MENU_Z_INDEX}>
                <MenuItem icon={<RefreshCw size={14} />} onClick={syncWithEstimate} isDisabled={isSyncing}>
                  Synchronizuj
                </MenuItem>
                <MenuItem icon={<ChevronRight size={14} />} onClick={handleNavigateToCostEstimate}>Przejdź do kosztorysu</MenuItem>
              </MenuList>
            </Menu>
          )}

          <Menu>
            <MenuButton as={Button} size="xs" rightIcon={<ChevronDown size={12} />} leftIcon={<Zap size={13} />} colorScheme="gray" variant="outline">
              Narzędzia
            </MenuButton>
            <MenuList zIndex={GANTT_MENU_Z_INDEX}>
              <MenuItem icon={<RefreshCw size={14} />} onClick={fetchSchedule}>Odśwież</MenuItem>
              {!compact && <MenuItem icon={<GitBranch size={14} />} onClick={() => setShowDependencies(!showDependencies)} fontWeight={showDependencies ? "semibold" : "normal"}>Zależności</MenuItem>}
              {!compact && <MenuItem icon={<CalendarX2 size={14} />} onClick={onToggleWeekends} fontWeight={hideWeekends ? "semibold" : "normal"}>Ukryj weekendy</MenuItem>}
              <MenuItem icon={<ChevronsDown size={14} />} onClick={() => expandAll(schedule?.stages ?? [])}>Rozwiń wszystko</MenuItem>
              <MenuItem icon={<ChevronsUp size={14} />} onClick={collapseAll}>Zwiń wszystko</MenuItem>
              {!compact && <MenuItem icon={<CalendarDays size={14} />} onClick={onScrollToToday}>Dziś</MenuItem>}
            </MenuList>
          </Menu>

          {!compact && (
            <ButtonGroup size="xs" isAttached variant="outline">
              {(["days", "weeks", "months"] as TimeScale[]).map(s => (
                <Button key={s} onClick={() => onTimeScaleChange(s)} colorScheme={timeScale === s ? "primary" : "gray"} variant={timeScale === s ? "solid" : "outline"}>
                  {s === "days" ? "Dni" : s === "weeks" ? "Tyg." : "Mies."}
                </Button>
              ))}
            </ButtonGroup>
          )}
        </HStack>
      )}
    </Box>
  );
}
