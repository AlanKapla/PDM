import { useGantt } from "./GanttContext";
import StageFormModal from "./modals/StageFormModal";
import StagesOrderModal from "./modals/StagesOrderModal";
import MoveStageModal from "./modals/MoveStageModal";
import WorkFormModal from "./modals/WorkFormModal";
import WorksOrderModal from "./modals/WorksOrderModal";
import MoveWorkModal from "./modals/MoveWorkModal";
import PeriodsModal from "./modals/PeriodsModal";
import AssignmentsModal from "./modals/AssignmentsModal";
import CommentsModal from "./modals/CommentsModal";
import DependenciesModal from "./modals/DependenciesModal";
import type { WorkScheduleStageWeb, WorkScheduleStageWorkWeb } from "../../types/workSchedule.types";

function findWorkInSchedule(stages: WorkScheduleStageWeb[], workId: string): WorkScheduleStageWorkWeb | null {
  for (const stage of stages) {
    const found = stage.works.find(w => w.id === workId);
    if (found) return found;
    const deep = findWorkInSchedule(stage.childStages ?? [], workId);
    if (deep) return deep;
  }
  return null;
}

/**
 * Ten komponent renderuje właściwy modal na podstawie stanu mobileModal z GanttContext.
 * Montujemy go zawsze – renderuje modały tylko wtedy, gdy są aktywne.
 */
export default function MobileModalsConnector() {
  const { mobileModal, closeMobileModal, schedule } = useGantt();

  if (!mobileModal) return null;

  switch (mobileModal.type) {
    case "stageForm":
      return (
        <StageFormModal
          isOpen
          onClose={closeMobileModal}
          parentStageId={mobileModal.stageId}
        />
      );

    case "renameStage":
      return (
        <StageFormModal
          isOpen
          onClose={closeMobileModal}
          renameStageId={mobileModal.stageId}
          initialName={mobileModal.initialName}
        />
      );
    case "stagesOrder":
      return <StagesOrderModal isOpen onClose={closeMobileModal} />;

    case "moveStage":
      return (
        <MoveStageModal
          isOpen
          onClose={closeMobileModal}
          stageId={mobileModal.stageId}
        />
      );

    case "workForm":
      return (
        <WorkFormModal
          isOpen
          onClose={closeMobileModal}
          stageId={mobileModal.stageId}
        />
      );

    case "editWork":
      return (
        <WorkFormModal
          isOpen
          onClose={closeMobileModal}
          stageId={mobileModal.stageId}
          editWork={mobileModal.work}
        />
      );

    case "worksOrder":
      return (
        <WorksOrderModal
          isOpen
          onClose={closeMobileModal}
          stageId={mobileModal.stageId}
        />
      );

    case "moveWork":
      return (
        <MoveWorkModal
          isOpen
          onClose={closeMobileModal}
          stageId={mobileModal.stageId}
          workId={mobileModal.workId}
        />
      );

    case "periods":
      return (
        <PeriodsModal
          isOpen
          onClose={closeMobileModal}
          stageId={mobileModal.stageId}
          work={mobileModal.work}
        />
      );

    case "assignments":
      return (
        <AssignmentsModal
          isOpen
          onClose={closeMobileModal}
          stageId={mobileModal.stageId}
          work={mobileModal.work}
        />
      );

    case "comments": {
      // Szukamy świeżego work z aktualnego harmonogramu — mobileModal.work to snapshot sprzed otwarcia
      const freshWork = findWorkInSchedule(schedule?.stages ?? [], mobileModal.work.id) ?? mobileModal.work;
      return (
        <CommentsModal
          isOpen
          onClose={closeMobileModal}
          stageId={mobileModal.stageId}
          work={freshWork}
        />
      );
    }

    case "dependencies":
      return <DependenciesModal isOpen onClose={closeMobileModal} />;

    default:
      return null;
  }
}
