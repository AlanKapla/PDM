import {
  Box,
  VStack,
  Text,
  Spinner,
  useColorModeValue,
  Button,
  HStack,
  IconButton,
  Tooltip,
} from "@chakra-ui/react";
import { Plus } from "lucide-react";
import { useGantt } from "./GanttContext";
import MobileStageRow from "./MobileStageRow";

export default function ScheduleMobileList() {
  const { schedule, isLoading, mode, canEdit, openMobileModal } = useGantt();
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const cardBg = useColorModeValue("white", "gray.800");

  const stages = [...(schedule?.stages ?? [])].sort((a, b) => a.order - b.order);

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
