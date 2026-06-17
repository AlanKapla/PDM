import {
  Box,
  VStack,
  Text,
  Spinner,
  useColorModeValue,
  Button,
} from "@chakra-ui/react";
import { Plus } from "lucide-react";
import { useMemo } from "react";
import { useGantt } from "./GanttContext";
import MobileStageRow from "./MobileStageRow";
import { filterStagesBySearch } from "./ganttRowUtils";

export default function ScheduleMobileList() {
  const { schedule, isLoading, mode, canEdit, openMobileModal, searchQuery } = useGantt();
  const borderColor = useColorModeValue("gray.200", "gray.700");

  const stages = useMemo(() => {
    const filtered = filterStagesBySearch(schedule?.stages ?? [], searchQuery);
    return [...filtered].sort((a, b) => a.order - b.order);
  }, [schedule?.stages, searchQuery]);

  if (isLoading) {
    return (
      <Box p={4} display="flex" justifyContent="center">
        <Spinner />
      </Box>
    );
  }

  return (
    <VStack spacing={0} align="stretch">
      {stages.map(stage => (
        <MobileStageRow key={stage.id} stage={stage} depth={0} />
      ))}

      {mode === "edit" && canEdit && (
        <Box p={3} borderTopWidth="1px" borderColor={borderColor}>
          <Button
            size="sm"
            leftIcon={<Plus size={14} />}
            colorScheme="green"
            variant="outline"
            w="full"
            onClick={() => openMobileModal({ type: "stageForm" })}
          >
            Dodaj etap
          </Button>
        </Box>
      )}
    </VStack>
  );
}
