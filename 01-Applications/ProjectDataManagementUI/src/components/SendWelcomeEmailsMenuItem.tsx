import { Fragment } from "react";
import { MenuItem, useDisclosure } from "@chakra-ui/react";
import { Mail } from "lucide-react";
import { useAuth } from "../context/AuthContext";
import { useSendWelcomeEmails } from "../hooks/useSendWelcomeEmails";
import DeleteAlertDialog from "./ui/DeleteAlertDialog";

export function SendWelcomeEmailsMenuItem(): React.ReactElement | null {
  const { user } = useAuth();
  const { isOpen, onOpen, onClose } = useDisclosure();
  const { mutateAsync: sendWelcomeEmails, isPending } = useSendWelcomeEmails();

  if (!user?.isSuperAdmin) {
    return null;
  }

  const handleConfirm = async (): Promise<void> => {
    await sendWelcomeEmails();
    onClose();
  };

  return (
    <Fragment>
      <MenuItem icon={<Mail size={16} aria-hidden />} onClick={onOpen}>
        Wyślij maile powitalne
      </MenuItem>

      <DeleteAlertDialog
        isOpen={isOpen}
        onClose={onClose}
        onConfirm={() => {
          void handleConfirm();
        }}
        title="Wysłać maile powitalne?"
        description="Wyśle maile powitalne do wszystkich aktywnych użytkowników, którzy jeszcze ich nie otrzymali. Tej operacji nie można cofnąć."
        confirmLabel="Wyślij"
        isLoading={isPending}
      />
    </Fragment>
  );
}
