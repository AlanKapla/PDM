import { useRef } from "react";
import {
  AlertDialog,
  AlertDialogBody,
  AlertDialogContent,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogOverlay,
  Button,
  Text,
  VStack,
  useBreakpointValue,
} from "@chakra-ui/react";
import type { WorkScheduleAssignmentConflictWeb } from "../../types/workSchedule.types";
import { formatAssigneeConflictLine } from "../../utils/detectAssigneeConflicts";

export interface AssignmentConflictAlertDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void;
  conflicts: WorkScheduleAssignmentConflictWeb[];
  isLoading?: boolean;
}

export function AssignmentConflictAlertDialog({
  isOpen,
  onClose,
  onConfirm,
  conflicts,
  isLoading = false,
}: AssignmentConflictAlertDialogProps): React.ReactElement {
  const cancelRef = useRef<HTMLButtonElement>(null);
  const modalSize = useBreakpointValue({ base: "full", md: "md" });

  return (
    <AlertDialog
      isOpen={isOpen}
      leastDestructiveRef={cancelRef}
      onClose={onClose}
      size={modalSize}
      isCentered
    >
      <AlertDialogOverlay>
        <AlertDialogContent>
          <AlertDialogHeader fontSize="lg" fontWeight="bold">
            Konflikt terminów
          </AlertDialogHeader>
          <AlertDialogBody>
            <Text mb={3} role="alert">
              Wybrane osoby lub kontrahenci są już przypisani do innej pracy w nakładającym się terminie.
            </Text>
            <VStack align="stretch" spacing={1} maxH="240px" overflowY="auto">
              {conflicts.map((conflict, index) => (
                <Text key={`${conflict.conflictingWorkId}-${conflict.assigneeName}-${index}`} fontSize="sm">
                  {formatAssigneeConflictLine(conflict)}
                </Text>
              ))}
            </VStack>
          </AlertDialogBody>
          <AlertDialogFooter gap={2}>
            <Button ref={cancelRef} variant="ghost" onClick={onClose} isDisabled={isLoading}>
              Anuluj
            </Button>
            <Button colorScheme="orange" onClick={onConfirm} isLoading={isLoading}>
              Przypisz mimo to
            </Button>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialogOverlay>
    </AlertDialog>
  );
}
