import React from "react";
import {
  Card,
  CardBody,
  CardHeader,
  Heading,
  HStack,
  VStack,
  Text,
  Switch,
  FormControl,
  FormLabel,
  useDisclosure,
  useToast,
} from "@chakra-ui/react";
import ConfirmAlertDialog from "../../../../components/ui/ConfirmAlertDialog";
import { useGrantFullAccess, useRevokeFullAccess } from "../../../../hooks/queries";
import type { TenantSubscription } from "../../../../types/subscription";

interface FullAccessToggleProps {
  subscription: TenantSubscription;
  tenantId: string;
}

export function FullAccessToggle({
  subscription,
  tenantId,
}: FullAccessToggleProps): React.ReactElement {
  const toast = useToast();
  const { isOpen, onOpen, onClose } = useDisclosure();
  const [pendingValue, setPendingValue] = React.useState<boolean>(false);

  const { mutate: grant, isPending: isGranting } = useGrantFullAccess();
  const { mutate: revoke, isPending: isRevoking } = useRevokeFullAccess();

  const isMutating = isGranting || isRevoking;

  function handleToggle(checked: boolean): void {
    setPendingValue(checked);
    onOpen();
  }

  function handleConfirm(): void {
    onClose();

    if (pendingValue) {
      grant(tenantId, {
        onSuccess: () => {
          toast({
            title: "Pełny dostęp przyznany",
            status: "success",
            duration: 3000,
            isClosable: true,
            position: "top-right",
          });
        },
        onError: () => {
          toast({
            title: "Błąd podczas przyznawania dostępu",
            status: "error",
            duration: 5000,
            isClosable: true,
            position: "top-right",
          });
        },
      });
    } else {
      revoke(tenantId, {
        onSuccess: () => {
          toast({
            title: "Pełny dostęp odebrany",
            status: "success",
            duration: 3000,
            isClosable: true,
            position: "top-right",
          });
        },
        onError: () => {
          toast({
            title: "Błąd podczas odbierania dostępu",
            status: "error",
            duration: 5000,
            isClosable: true,
            position: "top-right",
          });
        },
      });
    }
  }

  return (
    <>
      <Card variant="outline">
        <CardHeader pb={2}>
          <Heading size="sm">Full Access</Heading>
        </CardHeader>
        <CardBody pt={0}>
          <VStack align="stretch" spacing={3}>
            <FormControl>
              <HStack justify="space-between">
                <FormLabel fontSize="sm" mb={0}>
                  Pełny dostęp (bez limitów)
                </FormLabel>
                <Switch
                  isChecked={subscription.isFullAccess}
                  onChange={(e) => handleToggle(e.target.checked)}
                  isDisabled={isMutating}
                  colorScheme="green"
                />
              </HStack>
            </FormControl>

            {subscription.isFullAccess &&
              subscription.fullAccessGrantedAt && (
                <Text fontSize="xs" color="gray.500">
                  Przyznany:{" "}
                  {new Date(subscription.fullAccessGrantedAt).toLocaleString("pl-PL")}{" "}
                  przez {subscription.fullAccessGrantedByAdminId}
                </Text>
              )}
          </VStack>
        </CardBody>
      </Card>

      <ConfirmAlertDialog
        isOpen={isOpen}
        onClose={onClose}
        onConfirm={handleConfirm}
        title="Potwierdzenie"
        body={
          pendingValue
            ? "Czy na pewno chcesz przyznać pełny dostęp temu tenantowi?"
            : "Czy na pewno chcesz odebrać pełny dostęp?"
        }
        confirmLabel={pendingValue ? "Przyznaj" : "Odbierz"}
        confirmColorScheme={pendingValue ? "green" : "red"}
        isLoading={isMutating}
      />
    </>
  );
}
