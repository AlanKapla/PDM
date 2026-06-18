import React, { useState } from 'react';
import {
  Box,
  Flex,
  IconButton,
  SimpleGrid,
  Table,
  Tbody,
  Td,
  Text,
  Tfoot,
  Th,
  Thead,
  Tr,
  useToken,
} from '@chakra-ui/react';
import { Trash2 } from 'lucide-react';
import type { TrackedCostWeb } from '../../types/projectDashboard.types';
import { KpiCard } from '../shared/KpiCard';
import { NetGrossAmount } from '../shared/NetGrossAmount';
import { CostModal } from '../CostModal';
import AppModal from '../../../../components/ui/AppModal';
import { CostTimeSeriesChart } from '../charts/CostTimeSeriesChart';
import { CostSourceTypeChart } from '../charts/CostSourceTypeChart';
import { TopContractorsChart } from '../charts/TopContractorsChart';

export interface CostsTabProps {
  costs: TrackedCostWeb[];
  tenantId: string;
  projectId: string;
  onRefetch: () => void;
}

function sourceLabel(cost: TrackedCostWeb): React.ReactElement {
  const st = cost.sourceType;
  if (st === 'ScheduleWorkItem') {
    return (
      <Text as="span" fontSize="xs" bg="level2.50" color="level2.600" borderRadius="sm" px={2} py={0.5}>
        Harmonogram{cost.scheduleName ? `: ${cost.scheduleName}` : ''}
      </Text>
    );
  }
  if (st === 'EstimateItem') {
    return (
      <Text as="span" fontSize="xs" bg="action.50" color="level1.700" borderRadius="sm" px={2} py={0.5}>
        Kosztorys{cost.estimateName ? `: ${cost.estimateName}` : ''}
      </Text>
    );
  }
  if (st === 'LinkedWorkItem') {
    return (
      <Box>
        <Text as="span" fontSize="xs" bg="primary.50" color="primary.600" borderRadius="sm" px={2} py={0.5}>
          Powiązany
        </Text>
        {(cost.scheduleName ?? cost.estimateName) && (
          <Text fontSize="xs" color="neutral.600" mt={1}>
            {[cost.scheduleName, cost.estimateName].filter(Boolean).join(' + ')}
          </Text>
        )}
      </Box>
    );
  }
  return (
    <Text as="span" fontSize="xs" bg="neutral.50" color="neutral.600" borderRadius="sm" px={2} py={0.5}>
      Koszty główne
    </Text>
  );
}

function scopeLabel(cost: TrackedCostWeb): string {
  const st = cost.sourceType;
  if (st === 'ScheduleWorkItem' || st === 'LinkedWorkItem') {
    if (cost.stageName ?? cost.workItemName) {
      return `${cost.stageName ?? '—'} / ${cost.workItemName ?? '—'}`;
    }
  }
  if (st === 'EstimateItem' || st === 'LinkedWorkItem') {
    if (cost.estimateGroupName ?? cost.estimateItemName) {
      return `${cost.estimateGroupName ?? '—'} / ${cost.estimateItemName ?? '—'}`;
    }
  }
  return '—';
}

export function CostsTab({
  costs,
  tenantId,
  projectId,
  onRefetch,
}: CostsTabProps): React.ReactElement {
  const [editingCost, setEditingCost] = useState<TrackedCostWeb | null>(null);
  const [confirmDeleteCost, setConfirmDeleteCost] = useState<TrackedCostWeb | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [orange600] = useToken('colors', ['orange.600']);

  const totalNet = costs.reduce((sum, c) => sum + (c.net ?? 0), 0);
  const totalGross = costs.reduce((sum, c) => sum + (c.gross ?? 0), 0);
  const countSchedule = costs.filter(
    (c) => c.sourceType === 'ScheduleWorkItem' || c.sourceType === 'LinkedWorkItem'
  ).length;
  const countEstimate = costs.filter(
    (c) => c.sourceType === 'EstimateItem' || c.sourceType === 'LinkedWorkItem'
  ).length;
  const totalAdditionalNet = costs
    .filter((c) => c.sourceType === 'ProjectAdditional' || !c.sourceType)
    .reduce((sum, c) => sum + (c.net ?? 0), 0);
  const totalAdditionalGross = costs
    .filter((c) => c.sourceType === 'ProjectAdditional' || !c.sourceType)
    .reduce((sum, c) => sum + (c.gross ?? 0), 0);

  const handleDeleteConfirmed = async (): Promise<void> => {
    if (!confirmDeleteCost) {
      return;
    }
    setIsDeleting(true);
    try {
      const { deleteTrackedCost } = await import('../../services/dashboardApi');
      await deleteTrackedCost(tenantId, projectId, confirmDeleteCost.id);
      setConfirmDeleteCost(null);
      onRefetch();
    } catch {
      // błąd obsługiwany przez backend
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <Box w="100%">
      <SimpleGrid columns={{ base: 2, md: 5 }} spacing={3} mb={6}>
        <KpiCard label="Łączne koszty" netValue={totalNet} grossValue={totalGross} colorScheme="orange" />
        <KpiCard label="Liczba pozycji" value={String(costs.length)} colorScheme="primary" />
        <KpiCard label="Z harmonogramu" value={String(countSchedule)} colorScheme="level2" />
        <KpiCard label="Z kosztorysu" value={String(countEstimate)} colorScheme="level1" />
        <KpiCard label="Koszty główne" netValue={totalAdditionalNet} grossValue={totalAdditionalGross} colorScheme="amber" />
      </SimpleGrid>

      {costs.length > 0 && (
        <Box mb={4} display="flex" flexDirection="column" gap={3}>
          <CostTimeSeriesChart costs={costs} />
          <Box display="grid" gridTemplateColumns={{ base: '1fr', md: '1fr 1fr' }} gap={3}>
            <CostSourceTypeChart costs={costs} />
            <TopContractorsChart costs={costs} limit={10} title="Top 10 wykonawców" />
          </Box>
        </Box>
      )}

      <Box
        bg="white"
        borderWidth="2px"
        borderColor="neutral.200"
        borderRadius="xl"
        p={{ base: 4, md: 5 }}
        w="100%"
      >
        <Text fontSize="md" fontWeight="semibold" color="neutral.800" mb={4}>
          Lista kosztów
        </Text>

        <Box className="dashboard-table-wrap">
          {costs.length === 0 ? (
            <Box py={8} textAlign="center">
              <Text fontSize="sm" color="neutral.500" mb={1}>
                Brak kosztów w tym projekcie.
              </Text>
              <Text fontSize="xs" color="neutral.400" fontStyle="italic">
                Użyj przycisku „Dodaj koszt” u góry strony, aby dodać pierwszy koszt.
              </Text>
            </Box>
          ) : (
            <Table size="sm" variant="simple">
              <Thead>
                <Tr>
                  <Th>Nazwa / Nr</Th>
                  <Th>Źródło</Th>
                  <Th display={{ base: 'none', md: 'table-cell' }}>Etap / Zakres</Th>
                  <Th display={{ base: 'none', md: 'table-cell' }}>Wykonawca</Th>
                  <Th isNumeric>Kwota</Th>
                  <Th w="48px" />
                </Tr>
              </Thead>
              <Tbody>
                {costs.map((cost) => (
                  <Tr
                    key={cost.id}
                    cursor="pointer"
                    _hover={{ bg: 'neutral.50' }}
                    onClick={() => setEditingCost(cost)}
                  >
                    <Td>
                      <Text fontWeight="medium">{cost.name}</Text>
                      {cost.number && (
                        <Text fontSize="xs" color="neutral.400">
                          {cost.number}
                        </Text>
                      )}
                    </Td>
                    <Td>{sourceLabel(cost)}</Td>
                    <Td color="neutral.600" display={{ base: 'none', md: 'table-cell' }}>
                      {scopeLabel(cost)}
                    </Td>
                    <Td color="neutral.600" display={{ base: 'none', md: 'table-cell' }}>
                      {cost.contractorName ?? '—'}
                    </Td>
                    <Td isNumeric>
                      <NetGrossAmount
                        net={cost.net}
                        gross={cost.gross}
                        size="sm"
                        align="right"
                        accentColor="orange.600"
                      />
                    </Td>
                    <Td whiteSpace="nowrap" onClick={(e) => e.stopPropagation()}>
                      <IconButton
                        size="xs"
                        variant="ghost"
                        colorScheme="red"
                        aria-label="Usuń koszt"
                        icon={<Trash2 size={12} aria-hidden="true" />}
                        onClick={() => setConfirmDeleteCost(cost)}
                      />
                    </Td>
                  </Tr>
                ))}
              </Tbody>
              <Tfoot>
                <Tr>
                  <Td colSpan={6} borderTopWidth="1px" borderColor="neutral.200">
                    <Flex justify="flex-end" align="center" gap={2} fontWeight="medium" color="neutral.600">
                      <Text fontSize="sm">Suma łączna:</Text>
                      <NetGrossAmount
                        net={totalNet}
                        gross={totalGross}
                        size="sm"
                        align="right"
                        accentColor={orange600}
                      />
                    </Flex>
                  </Td>
                </Tr>
              </Tfoot>
            </Table>
          )}
        </Box>
      </Box>

      {editingCost && (
        <CostModal
          type="tracked"
          tenantId={tenantId}
          projectId={projectId}
          mode="edit"
          cost={editingCost}
          onSuccess={() => {
            onRefetch();
            setEditingCost(null);
          }}
          onClose={() => setEditingCost(null)}
        />
      )}

      {confirmDeleteCost && (
        <AppModal
          isOpen
          onClose={() => setConfirmDeleteCost(null)}
          title="Usuń koszt"
          actionLabel="Usuń"
          actionColorScheme="red"
          onAction={handleDeleteConfirmed}
          isActionLoading={isDeleting}
          desktopSize="sm"
        >
          <Text fontSize="sm">
            Czy na pewno chcesz usunąć <strong>{confirmDeleteCost.name}</strong>? Operacji nie można cofnąć.
          </Text>
        </AppModal>
      )}
    </Box>
  );
}

export default CostsTab;
