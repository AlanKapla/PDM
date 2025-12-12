import { useDisclosure as useChakraDisclosure } from "@chakra-ui/react";
import { useCallback } from "react";

export const useModal = (initialState = false) => {
  const { isOpen, onOpen, onClose: chakraOnClose } = useChakraDisclosure({ defaultIsOpen: initialState });

  const onClose = useCallback(() => {
    chakraOnClose();
  }, [chakraOnClose]);

  const toggle = useCallback(() => {
    if (isOpen) {
      onClose();
    } else {
      onOpen();
    }
  }, [isOpen, onClose, onOpen]);

  return {
    isOpen,
    onOpen,
    onClose,
    toggle,
  };
};
