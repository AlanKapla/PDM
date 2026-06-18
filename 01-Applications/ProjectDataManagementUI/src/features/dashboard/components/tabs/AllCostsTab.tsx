import React, { useState } from 'react';
import { useToken, Table, Thead, Tbody, Tfoot, Tr, Th, Td, IconButton, Box, SimpleGrid } from '@chakra-ui/react';
import { Pencil, Trash2, Sparkles } from 'lucide-react';
import { AICostImportModal } from '../../../../components/CostTracker/AICostImportModal';
import type { ParsedCostDto } from '../../../../types/ai.types';
import type { TrackedCostWeb } from '../../types/projectDashboard.types';
import { KpiCard } from '../shared/KpiCard';
import { NetGrossAmount } from '../shared/NetGrossAmount';
import { CostModal } from '../CostModal';
import AppModal from '../../../../components/ui/AppModal';
import { CostTimeSeriesChart } from '../charts/CostTimeSeriesChart';
import { CostSourceTypeChart } from '../charts/CostSourceTypeChart';
import { TopContractorsChart } from '../charts/TopContractorsChart';

export interface AllCostsTabProps {
  costs: TrackedCostWeb[];
  tenantId: string;
  projectId: string;
  onRefetch: () => void;
}

/**
 * Zakładka wszystkich kosztów projektu (spłaszczona lista z AllCosts).
 */
export function AllCostsTab({
  costs,
  tenantId,
  projectId,
  onRefetch,
}: AllCostsTabProps): React.ReactElement {
  const [editingCost, setEditingCost] = useState<TrackedCostWeb | null>(null);
  const [createModal, setCreateModal] = useState(false);
  const [aiImportModal, setAiImportModal] = useState(false);
  const [aiPrefillData, setAiPrefillData] = useState<{ parsedData: ParsedCostDto; file: File } | null>(null);
  const [confirmDeleteCost, setConfirmDeleteCost] = useState<TrackedCostWeb | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [
    neutral200, neutral400, neutral50, neutral600,
    level250, level2600, action50, level1700, level1500,
    primary50, primary600, orange600,
    red50, red600, red400,
  ] = useToken('colors', [
    'neutral.200', 'neutral.400', 'neutral.50', 'neutral.600',
    'level2.50', 'level2.600', 'action.50', 'level1.700', 'level1.500',
    'primary.50', 'primary.600', 'orange.600',
    'red.50', 'red.600', 'red.400',
  ]);

  const totalNet = costs.reduce((sum, c) => sum + (c.net ?? 0), 0);
  const totalGross = costs.reduce((sum, c) => sum + (c.gross ?? 0), 0);
  const countSchedule = costs.filter(
    (c) => c.sourceType === 'ScheduleWorkItem' || c.sourceType === 'LinkedWorkItem'
  ).length;
  const countEstimate = costs.filter(
    (c) => c.sourceType === 'EstimateItem' || c.sourceType === 'LinkedWorkItem'
  ).length;
  const countAdditional = costs.filter((c) => c.sourceType === 'ProjectAdditional' || !c.sourceType).length;
  const totalAdditionalNet = costs
    .filter((c) => c.sourceType === 'ProjectAdditional' || !c.sourceType)
    .reduce((sum, c) => sum + (c.net ?? 0), 0);
  const totalAdditionalGross = costs
    .filter((c) => c.sourceType === 'ProjectAdditional' || !c.sourceType)
    .reduce((sum, c) => sum + (c.gross ?? 0), 0);

  const handleDeleteConfirmed = async () => {
    if (!confirmDeleteCost) return;
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
    <div>
      <SimpleGrid columns={{ base: 2, md: 5 }} spacing={2} mb={4}>
        <KpiCard label="Łączne koszty" netValue={totalNet} grossValue={totalGross} />
        <KpiCard label="Liczba pozycji" value={String(costs.length)} />
        <KpiCard label="Z harmonogramu" value={String(countSchedule)} />
        <KpiCard label="Z kosztorysu" value={String(countEstimate)} />
        <KpiCard label="Koszty główne" netValue={totalAdditionalNet} grossValue={totalAdditionalGross} />
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

      <div
        style={{
          background: '#fff',
          border: `0.5px solid ${neutral200}`,
          borderRadius: 12,
          padding: 16,
        }}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
          <span style={{ fontSize: '0.875rem', fontWeight: 500 }}>Lista kosztów</span>
          <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
            <button
              onClick={() => setAiImportModal(true)}
              style={{
                fontSize: '0.75rem',
                padding: '6px 12px',
                background: 'linear-gradient(135deg, #7c3aed 0%, #a855f7 100%)',
                color: '#fff',
                border: 'none',
                borderRadius: 6,
                cursor: 'pointer',
                display: 'flex',
                alignItems: 'center',
                gap: 4,
                fontWeight: 500,
                boxShadow: '0 1px 4px rgba(124, 58, 237, 0.35)',
              }}
            >
              <Sparkles size={12} />
              Importuj z AI
            </button>
            <button
              onClick={() => setCreateModal(true)}
              style={{
                fontSize: '0.75rem',
                padding: '6px 12px',
                background: action50,
                color: level1700,
                border: `0.5px solid ${level1500}`,
                borderRadius: 6,
                cursor: 'pointer',
              }}
            >
              + Dodaj koszt
            </button>
          </div>
        </div>
        <div className="dashboard-table-wrap">
        {costs.length === 0 ? (
          <div style={{ padding: '32px 16px', textAlign: 'center' }}>
            <div style={{ fontSize: '0.875rem', color: neutral400, marginBottom: 6 }}>
              Brak kosztów w tym projekcie.
            </div>
            <div style={{ fontSize: '0.75rem', color: neutral400, fontStyle: 'italic' }}>
              Dodaj koszty przez zakładkę „Koszty główne” lub przez pozycje kosztorysu.
            </div>
          </div>
        ) : (
          <Table size="sm" variant="simple">
            <Thead>
              <Tr>
                <Th color="neutral.400" borderBottomWidth="0.5px" borderBottomColor="neutral.200" fontWeight="medium" px="6px" py="4px">Nazwa / Nr</Th>
                <Th color="neutral.400" borderBottomWidth="0.5px" borderBottomColor="neutral.200" fontWeight="medium" px="6px" py="4px">Źródło</Th>
                <Th color="neutral.400" borderBottomWidth="0.5px" borderBottomColor="neutral.200" fontWeight="medium" px="6px" py="4px" display={{ base: 'none', md: 'table-cell' }}>Etap / Zakres</Th>
                <Th color="neutral.400" borderBottomWidth="0.5px" borderBottomColor="neutral.200" fontWeight="medium" px="6px" py="4px" display={{ base: 'none', md: 'table-cell' }}>Wykonawca</Th>
                <Th isNumeric color="neutral.400" borderBottomWidth="0.5px" borderBottomColor="neutral.200" fontWeight="medium" px="6px" py="4px">Kwota</Th>
                <Th color="neutral.400" borderBottomWidth="0.5px" borderBottomColor="neutral.200" fontWeight="medium" px="6px" py="4px"></Th>
              </Tr>
            </Thead>
            <Tbody>
              {costs.map((cost) => (
                <Tr key={cost.id}>
                  <Td px="6px" py="4px">
                    <div style={{ fontWeight: "medium" }}>{cost.name}</div>
                    {cost.number && (
                      <div style={{ fontSize: "xs", color: neutral400 }}>{cost.number}</div>
                    )}
                  </Td>
                  <Td px="6px" py="4px">
                    {(() => {
                      const st = cost.sourceType;
                      if (st === 'ScheduleWorkItem') {
                        return (
                          <span
                            style={{
                              fontSize: "xs",
                              background: level250,
                              color: level2600,
                              borderRadius: 4,
                              padding: '2px 6px',
                              whiteSpace: 'nowrap',
                            }}
                          >
                            Harmonogram{cost.scheduleName ? `: ${cost.scheduleName}` : ''}
                          </span>
                        );
                      }
                      if (st === 'EstimateItem') {
                        return (
                          <span
                            style={{
                              fontSize: "xs",
                              background: action50,
                              color: level1700,
                              borderRadius: 4,
                              padding: '2px 6px',
                              whiteSpace: 'nowrap',
                            }}
                          >
                            Kosztorys{cost.estimateName ? `: ${cost.estimateName}` : ''}
                          </span>
                        );
                      }
                      if (st === 'LinkedWorkItem') {
                        return (
                          <div>
                            <span
                              style={{
                                fontSize: "xs",
                              background: primary50,
                              color: primary600,
                                borderRadius: 4,
                                padding: '2px 6px',
                                whiteSpace: 'nowrap',
                              }}
                            >
                              Powiązany
                            </span>
                            {(cost.scheduleName || cost.estimateName) && (
                              <div style={{ fontSize: "xs", color: neutral600, marginTop: 2 }}>
                                {[cost.scheduleName, cost.estimateName].filter(Boolean).join(' + ')}
                              </div>
                            )}
                          </div>
                        );
                      }
                      return (
                        <span
                          style={{
                              fontSize: "xs",
                              background: neutral50,
                              color: neutral600,
                            borderRadius: 4,
                            padding: '2px 6px',
                            whiteSpace: 'nowrap',
                          }}
                        >
                          Koszty główne
                        </span>
                      );
                    })()}
                  </Td>
                  <Td px="6px" py="4px" color="neutral.600" display={{ base: 'none', md: 'table-cell' }}>
                    {(() => {
                      const st = cost.sourceType;
                      if (st === 'ScheduleWorkItem' || st === 'LinkedWorkItem') {
                        if (cost.stageName || cost.workItemName) {
                          return `${cost.stageName ?? '—'} / ${cost.workItemName ?? '—'}`;
                        }
                      }
                      if (st === 'EstimateItem' || st === 'LinkedWorkItem') {
                        if (cost.estimateGroupName || cost.estimateItemName) {
                          return `${cost.estimateGroupName ?? '—'} / ${cost.estimateItemName ?? '—'}`;
                        }
                      }
                      return '—';
                    })()}
                  </Td>
                  <Td px="6px" py="4px" color="neutral.600" display={{ base: 'none', md: 'table-cell' }}>
                    {cost.contractorName ?? '—'}
                  </Td>
                  <Td isNumeric px="6px" py="4px">
                    <NetGrossAmount
                      net={cost.net}
                      gross={cost.gross}
                      size="sm"
                      align="right"
                      accentColor="orange.600"
                    />
                  </Td>
                  <Td px="6px" py="4px" whiteSpace="nowrap">
                    <IconButton
                      size="xs"
                      variant="ghost"
                      colorScheme="gray"
                      aria-label="Edytuj"
                      icon={<Pencil size={12} />}
                      onClick={() => setEditingCost(cost)}
                      mr={1}
                    />
                    <IconButton
                      size="xs"
                      variant="ghost"
                      colorScheme="red"
                      aria-label="Usuń"
                      icon={<Trash2 size={12} />}
                      onClick={() => setConfirmDeleteCost(cost)}
                    />
                  </Td>
                </Tr>
              ))}
            </Tbody>
            <Tfoot>
              <Tr>
                <Td
                  colSpan={6}
                  px="6px"
                  py="6px"
                  borderTopWidth="0.5px"
                  borderTopColor="neutral.200"
                  color="neutral.600"
                >
                  <div style={{ display: 'flex', justifyContent: 'flex-end', alignItems: 'center', gap: 8, fontWeight: 'medium' }}>
                    <span>Suma łączna:</span>
                    <NetGrossAmount
                      net={totalNet}
                      gross={totalGross}
                      size="sm"
                      align="right"
                      accentColor={orange600}
                    />
                  </div>
                </Td>
              </Tr>
            </Tfoot>
          </Table>
        )}
        </div>
      </div>

      {aiImportModal && (
        <AICostImportModal
          isOpen
          onClose={() => setAiImportModal(false)}
          tenantId={tenantId}
          projectId={projectId}
          costType="TrackedCost"
          onParsed={(data, file) => {
            setAiPrefillData({ parsedData: data, file });
            setAiImportModal(false);
            setCreateModal(true);
          }}
        />
      )}

      {createModal && (
        <CostModal
          type="tracked"
          tenantId={tenantId}
          projectId={projectId}
          mode="create"
          aiPrefill={aiPrefillData ?? undefined}
          onSuccess={() => { onRefetch(); setCreateModal(false); setAiPrefillData(null); }}
          onClose={() => { setCreateModal(false); setAiPrefillData(null); }}
        />
      )}

      {editingCost && (
        <CostModal
          type="tracked"
          tenantId={tenantId}
          projectId={projectId}
          mode="edit"
          cost={editingCost}
          onSuccess={() => { onRefetch(); setEditingCost(null); }}
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
          <span style={{ fontSize: "sm" }}>
            Czy na pewno chcesz usunąć <strong>{confirmDeleteCost.name}</strong>? Operacji nie można cofnąć.
          </span>
        </AppModal>
      )}
    </div>
  );
}

export default AllCostsTab;
