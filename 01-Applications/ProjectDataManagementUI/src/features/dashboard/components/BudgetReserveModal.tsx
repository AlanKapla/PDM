import React, { useState } from 'react';
import {
  VStack,
  FormControl,
  FormLabel,
  Input,
  Alert,
  AlertIcon,
} from '@chakra-ui/react';
import AppModal from '../../../components/ui/AppModal';
import type { UpdateTrackerBudgetRequest } from '../types/projectDashboard.types';
import { useTrackedCostMutations } from '../hooks/useTrackedCostMutations';

export interface BudgetReserveModalProps {
  tenantId: string;
  projectId: string;
  currentBudgetNet: number | null;
  currentBudgetGross: number | null;
  onSuccess: () => void;
  onClose: () => void;
}

/** Modal do edycji rezerwy budżetowej projektu. */
export function BudgetReserveModal({
  tenantId,
  projectId,
  currentBudgetNet,
  currentBudgetGross,
  onSuccess,
  onClose,
}: BudgetReserveModalProps): React.ReactElement {
  const { updateBudget, isLoading, error } = useTrackedCostMutations({
    tenantId,
    projectId,
  });

  const [budgetNet, setBudgetNet] = useState(
    currentBudgetNet != null ? String(currentBudgetNet) : ''
  );
  const [budgetGross, setBudgetGross] = useState(
    currentBudgetGross != null ? String(currentBudgetGross) : ''
  );

  const handleAction = async () => {
    const data: UpdateTrackerBudgetRequest = {
      budgetNet: budgetNet !== '' ? parseFloat(budgetNet) : null,
      budgetGross: budgetGross !== '' ? parseFloat(budgetGross) : null,
    };
    try {
      await updateBudget(data);
      onSuccess();
      onClose();
    } catch {
      // błąd wyświetlany z hooka
    }
  };

  return (
    <AppModal
      isOpen
      onClose={onClose}
      title="Edytuj budżet główny"
      actionLabel="Zapisz budżet główny"
      actionColorScheme="green"
      onAction={handleAction}
      isActionLoading={isLoading}
      desktopSize="sm"
    >
      <VStack spacing={4} align="stretch" sx={{ 'input, textarea, select': { fontSize: '16px' } }}>
        <FormControl>
          <FormLabel>Budżet główny netto (zł)</FormLabel>
          <Input
            type="number"
            step="0.01"
            value={budgetNet}
            onChange={(e) => setBudgetNet(e.target.value)}
          />
        </FormControl>

        <FormControl>
          <FormLabel>Budżet główny brutto (zł)</FormLabel>
          <Input
            type="number"
            step="0.01"
            value={budgetGross}
            onChange={(e) => setBudgetGross(e.target.value)}
          />
        </FormControl>

        {error && (
          <Alert status="error" borderRadius="md">
            <AlertIcon />
            {error}
          </Alert>
        )}
      </VStack>
    </AppModal>
  );
}

export default BudgetReserveModal;
