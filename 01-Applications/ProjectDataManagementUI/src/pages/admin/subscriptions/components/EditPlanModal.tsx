import React from "react";
import {
  VStack,
  HStack,
  FormControl,
  FormLabel,
  Input,
  NumberInput,
  NumberInputField,
  Switch,
  Text,
  useToast,
} from "@chakra-ui/react";
import AppModal from "../../../../components/ui/AppModal";
import { useUpdatePlanDefinition } from "../../../../hooks/queries";
import {
  PlanLabels,
  type PlanDefinition,
  type UpdatePlanDefinitionRequest,
} from "../../../../types/subscription";

interface EditPlanModalProps {
  plan: PlanDefinition | null;
  isOpen: boolean;
  onClose: () => void;
}

export function EditPlanModal({
  plan,
  isOpen,
  onClose,
}: EditPlanModalProps): React.ReactElement {
  const toast = useToast();
  const { mutate: updatePlan, isPending } = useUpdatePlanDefinition();

  const [name, setName] = React.useState("");
  const [maxProjects, setMaxProjects] = React.useState(0);
  const [maxUsers, setMaxUsers] = React.useState(0);
  const [price, setPrice] = React.useState(0);
  const [currency, setCurrency] = React.useState("PLN");
  const [isActive, setIsActive] = React.useState(true);

  React.useEffect(() => {
    if (plan) {
      setName(plan.name);
      setMaxProjects(plan.maxProjects);
      setMaxUsers(plan.maxUsers);
      setPrice(plan.price);
      setCurrency(plan.currency);
      setIsActive(plan.isActive);
    }
  }, [plan]);

  function handleSave(): void {
    if (!plan) return;

    const data: UpdatePlanDefinitionRequest = {
      name,
      maxProjects,
      maxUsers,
      price,
      currency,
      isActive,
    };

    updatePlan(
      { plan: plan.plan, data },
      {
        onSuccess: () => {
          toast({ title: "Plan zaktualizowany", status: "success", duration: 3000, isClosable: true, position: "top-right" });
          onClose();
        },
        onError: () => {
          toast({ title: "Błąd podczas zapisywania", status: "error", duration: 5000, isClosable: true, position: "top-right" });
        },
      },
    );
  }

  if (!plan) return <></>;

  return (
    <AppModal
      isOpen={isOpen}
      onClose={onClose}
      title={`Edytuj plan: ${PlanLabels[plan.plan]}`}
      actionLabel="Zapisz"
      onAction={handleSave}
      isActionLoading={isPending}
    >
      <VStack spacing={4} align="stretch">
        <FormControl isRequired>
          <FormLabel fontSize="sm">Nazwa wyświetlana</FormLabel>
          <Input
            size="sm"
            value={name}
            onChange={(e) => setName(e.target.value)}
          />
        </FormControl>

        <HStack spacing={4}>
          <FormControl isRequired>
            <FormLabel fontSize="sm">Max projektów (-1 = bez limitu)</FormLabel>
            <NumberInput
              size="sm"
              value={maxProjects}
              onChange={(_, val) => setMaxProjects(isNaN(val) ? 0 : val)}
              min={-1}
            >
              <NumberInputField />
            </NumberInput>
          </FormControl>

          <FormControl isRequired>
            <FormLabel fontSize="sm">Max użytkowników (-1 = bez limitu)</FormLabel>
            <NumberInput
              size="sm"
              value={maxUsers}
              onChange={(_, val) => setMaxUsers(isNaN(val) ? 0 : val)}
              min={-1}
            >
              <NumberInputField />
            </NumberInput>
          </FormControl>
        </HStack>

        <HStack spacing={4}>
          <FormControl isRequired>
            <FormLabel fontSize="sm">Cena miesięczna netto</FormLabel>
            <NumberInput
              size="sm"
              value={price}
              onChange={(_, val) => setPrice(isNaN(val) ? 0 : val)}
              min={0}
              precision={2}
            >
              <NumberInputField />
            </NumberInput>
          </FormControl>

          <FormControl isRequired>
            <FormLabel fontSize="sm">Waluta</FormLabel>
            <Input
              size="sm"
              value={currency}
              onChange={(e) => setCurrency(e.target.value)}
              maxLength={8}
            />
          </FormControl>
        </HStack>

        <FormControl>
          <HStack justify="space-between">
            <FormLabel fontSize="sm" mb={0}>
              Aktywny (widoczny dla użytkowników)
            </FormLabel>
            <Switch
              isChecked={isActive}
              onChange={(e) => setIsActive(e.target.checked)}
              colorScheme="green"
            />
          </HStack>
        </FormControl>

        <Text fontSize="xs" color="gray.500">
          Typ planu ({PlanLabels[plan.plan]}) jest niezmienny — identyfikuje plan w systemie.
        </Text>
      </VStack>
    </AppModal>
  );
}
