import { useContext, useCallback } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { Box, useBreakpointValue } from "@chakra-ui/react";
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
  const permissions = useResourcePermissions(projectId);

  const isMobile = useBreakpointValue({ base: true, md: false }) ?? false;

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

  return (
    <MainLayout>
      <GanttProvider
        tenantId={tenantId}
        projectId={resolvedProjectId}
        workScheduleId={resolvedWorkScheduleId}
        permissions={permissions}
        onAfterInitialLoad={scrollToTodayMinus3}
      >
        <GanttToolbar
          onNavigateBack={handleNavigateBack}
          timeScale={timeScale}
          onTimeScaleChange={setTimeScale}
          onScrollToToday={scrollToToday}
          hideWeekends={hideWeekends}
          onToggleWeekends={toggleWeekends}
          compact={isMobile}
        />

        {!isMobile && (
          <Box mt={2}>
            <GanttLayout
              dates={dates}
              dateGroups={dateGroups}
              timeScale={timeScale}
              columnWidth={columnWidth}
              hideWeekends={hideWeekends}
              scrollContainerRef={scrollContainerRef}
            />
          </Box>
        )}

        {isMobile && (
          <Box mt={2}>
            <ScheduleMobileList />
          </Box>
        )}

        <MobileModalsConnector />
      </GanttProvider>
    </MainLayout>
  );
}

