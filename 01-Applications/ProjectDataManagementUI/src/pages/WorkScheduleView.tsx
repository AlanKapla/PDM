import { useContext, useCallback, useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { Box, useBreakpointValue, useColorModeValue } from "@chakra-ui/react";
import MainLayout from "../layout/MainLayout";
import { GanttProvider } from "../components/gantt/GanttContext";
import GanttToolbar from "../components/gantt/GanttToolbar";
import GanttLayout from "../components/gantt/GanttLayout";
import ScheduleMobileList from "../components/gantt/ScheduleMobileList";
import MobileModalsConnector from "../components/gantt/MobileModalsConnector";
import { AuthContext } from "../context/AuthContext";
import { useResourcePermissions } from "../hooks/useResourcePermissions";
import { useTimelineData } from "../hooks/useTimelineData";

const COLUMN_WIDTHS = {
  days: 34,
  weeks: 26,
  months: 18,
} as const;

export default function WorkScheduleView() {
  const { projectId, workScheduleId } = useParams<{ projectId: string; workScheduleId: string }>();
  const navigate = useNavigate();
  const { user } = useContext(AuthContext);
  const permissions = useResourcePermissions(projectId, "schedule");

  const isMobile = useBreakpointValue({ base: true, md: false }) ?? false;
  const [searchQuery, setSearchQuery] = useState("");
  const [isFullscreen, setIsFullscreen] = useState(false);
  const pageBg = useColorModeValue("gray.50", "gray.900");

  const tenantId = user?.activeTenantId ?? "";
  const resolvedProjectId = projectId ?? "";
  const resolvedWorkScheduleId = workScheduleId ?? "";

  const {
    dates,
    dateGroups,
    timeScale,
    setTimeScale,
    hideWeekends,
    toggleWeekends,
    scrollContainerRef,
  } = useTimelineData({ isMobile });

  const columnWidth = COLUMN_WIDTHS[timeScale];

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === "Escape" && isFullscreen) {
        setIsFullscreen(false);
      }
    };
    document.addEventListener("keydown", handler);
    return () => {
      document.removeEventListener("keydown", handler);
    };
  }, [isFullscreen]);

  const handleNavigateBack = () => {
    if (resolvedProjectId) {
      navigate(`/projects/${resolvedProjectId}`);
    } else {
      navigate(-1);
    }
  };

  /** Przewija siatkę do today-3 dni — wywoływane przez GanttProvider po pierwszym załadowaniu */
  const scrollToTodayMinus3 = useCallback(() => {
    const now = new Date();
    const target = new Date(now);
    target.setDate(target.getDate() - 3);
    const targetIdx = dates.findIndex(
      d => d.getFullYear() === target.getFullYear() && d.getMonth() === target.getMonth() && d.getDate() === target.getDate(),
    );
    const scrollIdx = targetIdx >= 0 ? targetIdx : dates.findIndex(
      d => d.getFullYear() === now.getFullYear() && d.getMonth() === now.getMonth() && d.getDate() === now.getDate(),
    );
    if (scrollIdx >= 0 && scrollContainerRef.current) {
      scrollContainerRef.current.scrollLeft = scrollIdx * columnWidth;
    }
  }, [dates, columnWidth, scrollContainerRef]);

  /** Przewija siatkę do kolumny z dzisiejszą datą (przycisk w toolbarze) */
  const scrollToToday = useCallback(() => {
    const now = new Date();
    const todayIdx = dates.findIndex(
      d => d.getFullYear() === now.getFullYear() && d.getMonth() === now.getMonth() && d.getDate() === now.getDate(),
    );
    if (todayIdx >= 0 && scrollContainerRef.current) {
      scrollContainerRef.current.scrollLeft =
        todayIdx * columnWidth - scrollContainerRef.current.clientWidth / 2 + columnWidth / 2;
    }
  }, [dates, columnWidth, scrollContainerRef]);

  const scheduleContent = (
    <GanttProvider
      tenantId={tenantId}
      projectId={resolvedProjectId}
      workScheduleId={resolvedWorkScheduleId}
      permissions={permissions}
      onAfterInitialLoad={scrollToTodayMinus3}
      searchQuery={searchQuery}
      onSearchChange={setSearchQuery}
    >
      <GanttToolbar
        onNavigateBack={handleNavigateBack}
        timeScale={timeScale}
        onTimeScaleChange={setTimeScale}
        onScrollToToday={scrollToToday}
        hideWeekends={hideWeekends}
        onToggleWeekends={toggleWeekends}
        searchQuery={searchQuery}
        onSearchChange={setSearchQuery}
        compact={isMobile}
        isFullscreen={isFullscreen}
        onToggleFullscreen={() => setIsFullscreen((v) => !v)}
      />

      <Box
        mt={isFullscreen ? 0 : 2}
        flex={isFullscreen ? 1 : undefined}
        minH={isFullscreen ? 0 : undefined}
        overflow={isFullscreen ? "hidden" : undefined}
        display={isFullscreen ? "flex" : undefined}
        flexDirection={isFullscreen ? "column" : undefined}
      >
        {!isMobile && (
          <GanttLayout
            dates={dates}
            dateGroups={dateGroups}
            timeScale={timeScale}
            columnWidth={columnWidth}
            hideWeekends={hideWeekends}
            scrollContainerRef={scrollContainerRef}
            height={isFullscreen ? "100%" : "calc(100vh - 140px)"}
          />
        )}

        {isMobile && (
          <ScheduleMobileList />
        )}
      </Box>

      <MobileModalsConnector />
    </GanttProvider>
  );

  if (isFullscreen) {
    return (
      <Box
        position="fixed"
        top={0}
        left={0}
        right={0}
        bottom={0}
        bg={pageBg}
        zIndex={9999}
        display="flex"
        flexDirection="column"
      >
        {scheduleContent}
      </Box>
    );
  }

  return (
    <MainLayout>
      {scheduleContent}
    </MainLayout>
  );
}
