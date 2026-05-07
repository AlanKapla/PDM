import React, { useState } from 'react';
import { useToken, Table, Thead, Tbody, Tfoot, Tr, Th, Td, IconButton } from '@chakra-ui/react';
import { Pencil, Trash2 } from 'lucide-react';
import type { TrackedCostWeb } from '../../types/projectDashboard.types';
import { PLN } from '../../utils/formatters';
import { KpiCard } from '../shared/KpiCard';
import { TrackedCostModal } from '../TrackedCostModal';
import AppModal from '../../../../components/ui/AppModal';
import { useDashboardCurrency } from '../../context/DashboardCurrencyContext';

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
  const [confirmDeleteCost, setConfirmDeleteCost] = useState<TrackedCostWeb | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const currencySymbol = useDashboardCurrency();
  const [
    neutral200, neutral400, neutral50, neutral600,
    level250, level2600, action50, level1700,
    primary50, primary600, orange600,
    red50, red600, red400,
  ] = useToken('colors', [
    'neutral.200', 'neutral.400', 'neutral.50', 'neutral.600',
    'level2.50', 'level2.600', 'action.50', 'level1.700',
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
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(130px, 1fr))',
          gap: 8,
          marginBottom: 16,
        }}
      >
        <KpiCard label="Łączne koszty" value={PLN(totalNet, currencySymbol)} />
        <KpiCard label="Liczba pozycji" value={String(costs.length)} />
        <KpiCard label="Z harmonogramu" value={String(countSchedule)} />
        <KpiCard label="Z kosztorysu" value={String(countEstimate)} />
        <KpiCard label="Koszty główne" value={String(countAdditional)} />
      </div>

      <div
        style={{
          background: '#fff',
          border: `0.5px solid ${neutral200}`,
          borderRadius: 12,
          padding: 16,
        }}
      >
        <div className="dashboard-table-wrap">
        {costs.length === 0 ? (
          <div style={{ fontSize: "xs", color: neutral400, fontStyle: 'italic' }}>
            Brak kosztów
          </div>
        ) : (
          <Table size="sm" variant="simple">
            <Thead>
              <Tr>
                <Th color="neutral.400" borderBottomWidth="0.5px" borderBottomColor="neutral.200" fontWeight="medium" px="6px" py="4px">Nazwa / Nr</Th>
                <Th color="neutral.400" borderBottomWidth="0.5px" borderBottomColor="neutral.200" fontWeight="medium" px="6px" py="4px">Źródło</Th>
                <Th color="neutral.400" borderBottomWidth="0.5px" borderBottomColor="neutral.200" fontWeight="medium" px="6px" py="4px" display={{ base: 'none', md: 'table-cell' }}>Etap / Zakres</Th>
                <Th color="neutral.400" borderBottomWidth="0.5px" borderBottomColor="neutral.200" fontWeight="medium" px="6px" py="4px" display={{ base: 'none', md: 'table-cell' }}>Wykonawca</Th>
                <Th isNumeric color="neutral.400" borderBottomWidth="0.5px" borderBottomColor="neutral.200" fontWeight="medium" px="6px" py="4px">Netto</Th>
                <Th isNumeric color="neutral.400" borderBottomWidth="0.5px" borderBottomColor="neutral.200" fontWeight="medium" px="6px" py="4px">Brutto</Th>
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
                    {cost.contractor ?? '—'}
                  </Td>
                  <Td isNumeric px="6px" py="4px" color="orange.600" fontWeight="medium">
                    {PLN(cost.net, currencySymbol)}
                  </Td>
                  <Td isNumeric px="6px" py="4px" color="neutral.600">
                    {PLN(cost.gross, currencySymbol)}
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
                  colSpan={7}
                  px="6px"
                  py="6px"
                  borderTopWidth="0.5px"
                  borderTopColor="neutral.200"
                  color="neutral.600"
                  textAlign="right"
                  fontWeight="medium"
                >
                  Suma łączna: <span style={{ color: orange600 }}>{PLN(totalNet, currencySymbol)}</span>
                </Td>
              </Tr>
            </Tfoot>
          </Table>
        )}
        </div>
      </div>

      {editingCost && (
        <TrackedCostModal
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
