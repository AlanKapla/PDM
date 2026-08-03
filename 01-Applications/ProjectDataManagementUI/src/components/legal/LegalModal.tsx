import type React from "react";
import "@pdm-shared/legal/legalContent.css";
import AppModal from "../ui/AppModal";

export interface LegalModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  children: React.ReactNode;
}

export function LegalModal({
  isOpen,
  onClose,
  title,
  children,
}: LegalModalProps): React.ReactElement {
  return (
    <AppModal
      isOpen={isOpen}
      onClose={onClose}
      title={title}
      hideFooter
      desktopSize="2xl"
    >
      {children}
    </AppModal>
  );
}
