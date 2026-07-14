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
  useBreakpointValue,
} from "@chakra-ui/react";

interface DeleteAlertDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void;
  /** Opcjonalna nazwa usuwanego elementu wyświetlana w treści */
  itemName?: string;
  /** Opcjonalny tytuł dialogu */
  title?: string;
  /** Opcjonalny opis — zastępuje domyślną treść */
  description?: string;
  /** Etykieta przycisku potwierdzenia */
  confirmLabel?: string;
  isLoading?: boolean;
}

/**
 * Jednolity dialog potwierdzenia usunięcia dla całej aplikacji.
 * Oparty na Chakra UI AlertDialog (poprawna dostępność — focus wraca na cancelRef).
 */
export default function DeleteAlertDialog({
  isOpen,
  onClose,
  onConfirm,
  itemName,
  title = "Czy na pewno?",
  description,
  confirmLabel = "Usuń",
  isLoading = false,
}: DeleteAlertDialogProps) {
  const cancelRef = useRef<HTMLButtonElement>(null);
  const modalSize = useBreakpointValue({ base: "full", md: "md" });
  const isMobile = useBreakpointValue({ base: true, md: false });

  const bodyText = description ?? (
    <>
      {itemName ? `Czy na pewno chcesz usunąć "${itemName}"? ` : ""}
      Tej operacji nie można cofnąć.
    </>
  );

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

          <AlertDialogBody>
            <Text>{bodyText}</Text>
          </AlertDialogBody>

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
              colorScheme="red"
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
