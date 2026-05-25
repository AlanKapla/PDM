import { useRef } from "react";
import {
  AlertDialog,
  AlertDialogBody,
  AlertDialogContent,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogOverlay,
  Button,
  useBreakpointValue,
} from "@chakra-ui/react";

interface ConfirmAlertDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void;
  title?: string;
  body: string;
  confirmLabel?: string;
  confirmColorScheme?: string;
  isLoading?: boolean;
}

export default function ConfirmAlertDialog({
  isOpen,
  onClose,
  onConfirm,
  title = "Czy na pewno?",
  body,
  confirmLabel = "Potwierdź",
  confirmColorScheme = "red",
  isLoading = false,
}: ConfirmAlertDialogProps) {
  const cancelRef = useRef<HTMLButtonElement>(null);
  const modalSize = useBreakpointValue({ base: "full", md: "md" });
  const isMobile = useBreakpointValue({ base: true, md: false });

  return (
    <AlertDialog
      isOpen={isOpen}
      leastDestructiveRef={cancelRef}
      onClose={onClose}
      isCentered={!isMobile}
      size={modalSize}
    >
      <AlertDialogOverlay>
        <AlertDialogContent
          borderRadius={{ base: 0, md: "md" }}
          my={{ base: 0, md: "auto" }}
        >
          <AlertDialogHeader fontSize="lg" fontWeight="semibold">
            {title}
          </AlertDialogHeader>

          <AlertDialogBody>{body}</AlertDialogBody>

          <AlertDialogFooter gap={2}>
            <Button
              ref={cancelRef}
              variant="ghost"
              onClick={onClose}
              isDisabled={isLoading}
            >
              Anuluj
            </Button>
            <Button
              colorScheme={confirmColorScheme}
              variant="outline"
              onClick={onConfirm}
              isLoading={isLoading}
              ml={3}
            >
              {confirmLabel}
            </Button>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialogOverlay>
    </AlertDialog>
  );
}
