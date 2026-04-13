import {
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalFooter,
  ModalCloseButton,
  Divider,
  Button,
  HStack,
  Text,
  Box,
  useBreakpointValue,
} from "@chakra-ui/react";
import type { ModalProps } from "@chakra-ui/react";

interface AppModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  /** Tekst przycisku akcji (zapis/dodaj) */
  actionLabel?: string;
  /** Schemat kolorów przycisku akcji, domyślnie "green" */
  actionColorScheme?: string;
  onAction?: () => void;
  isActionLoading?: boolean;
  isActionDisabled?: boolean;
  cancelLabel?: string;
  /** Rozmiar modala na desktop, domyślnie "lg" */
  desktopSize?: ModalProps["size"];
  /** Czy ukryć stopkę (np. dla modali tylko z formularzem) */
  hideFooter?: boolean;
  /** Czy ukryć przycisk Anuluj w stopce */
  hideCancelButton?: boolean;
  children: React.ReactNode;
}

/**
 * Jednolity wrapper modala dla całej aplikacji.
 * - Mobile: pełen ekran (size="full"), stopka przyklejona do dołu
 * - Desktop: isCentered, rozmiar konfigurowalny (domyślnie "lg")
 */
export default function AppModal({
  isOpen,
  onClose,
  title,
  actionLabel,
  actionColorScheme = "green",
  onAction,
  isActionLoading = false,
  isActionDisabled = false,
  cancelLabel = "Anuluj",
  desktopSize = "lg",
  hideFooter = false,
  hideCancelButton = false,
  children,
}: AppModalProps) {
  const modalSize = useBreakpointValue({ base: "full", md: desktopSize });
  const isMobile = useBreakpointValue({ base: true, md: false });

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      size={modalSize}
      isCentered={!isMobile}
      closeOnOverlayClick={!isMobile}
      scrollBehavior="inside"
    >
      <ModalOverlay />
      <ModalContent
        borderRadius={{ base: 0, md: "md" }}
        my={{ base: 0, md: "auto" }}
        maxH={{ base: "100dvh", md: "90vh" }}
      >
        {/* Nagłówek: tytuł po lewej, X po prawej */}
        <ModalHeader>
          <Text fontSize="lg" fontWeight="semibold" pr={8}>
            {title}
          </Text>
        </ModalHeader>
        <ModalCloseButton top={3} right={3} />
        <Divider />

        {/* Treść */}
        <ModalBody py={4}>{children}</ModalBody>

        {/* Stopka: przyklejona do dołu na mobile */}
        {!hideFooter && (
          <>
            <Divider />
            <ModalFooter
              position={isMobile ? "sticky" : "relative"}
              bottom={0}
              bg="inherit"
              borderBottomRadius={{ md: "md" }}
              gap={2}
            >
              <HStack w="full" justify="space-between">
                {!hideCancelButton ? (
                  <Button variant="ghost" onClick={onClose} isDisabled={isActionLoading}>
                    {cancelLabel}
                  </Button>
                ) : (
                  <Box />
                )}
                {actionLabel && onAction && (
                  <Button
                    colorScheme={actionColorScheme}
                    onClick={onAction}
                    isLoading={isActionLoading}
                    isDisabled={isActionDisabled}
                    minH="44px"
                  >
                    {actionLabel}
                  </Button>
                )}
              </HStack>
            </ModalFooter>
          </>
        )}
      </ModalContent>
    </Modal>
  );
}
