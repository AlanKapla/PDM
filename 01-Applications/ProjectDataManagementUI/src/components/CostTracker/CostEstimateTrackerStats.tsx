import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, ChevronRight, ChevronDown, TrendingUp, AlertTriangle, List, Pencil, Trash2 } from 'lucide-react';
import { TrackedCostItemStatus, type TrackerGroupWeb, type TrackedCostWeb } from '../../types/costTracker.types';
import { costTrackerApi } from '../../api/costTrackerApi';
import { ItemStatusBadge } from './ItemStatusBadge';
import { DeviationCell } from './DeviationCell';
import { CoverageMiniBar } from './CoverageMiniBar';
import { MoneyCell } from './MoneyCell';
import { TrackedCostModal } from './TrackedCostModal';

function worstGroupStatus(group: TrackerGroupWeb): TrackedCostItemStatus {
  const itemStatuses = group.items.map((i) => i.status);
  const childStatuses = (group.childGroups ?? []).map(worstGroupStatus);
  const all = [...itemStatuses, ...childStatuses];
  if (all.length === 0) return TrackedCostItemStatus.NoCosts;
  return all.reduce((worst, s) => (s > worst ? s : worst), TrackedCostItemStatus.NoCosts);
}

interface CostEstimateTrackerStatsProps {
  tenantId: string;
  projectId: string;
  costEstimateId: string;
  currencySymbol?: string;
  trackerId?: string;
}

export const CostEstimateTrackerStats: React.FC<CostEstimateTrackerStatsProps> = ({
  tenantId,
  projectId,
  costEstimateId,
  currencySymbol = 'PLN',
  trackerId,
}) => {
  const [addCostContext, setAddCostContext] = useState<{
    costEstimateId?: string;
    costEstimateItemId?: string;
    label?: string;
  } | null>(null);

  const [editCost, setEditCost] = useState<TrackedCostWeb | null>(null);

  const [collapsed, setCollapsed] = useState<Record<string, boolean>>({});
  const toggleGroup = (groupId: string) =>
    setCollapsed((prev) => ({ ...prev, [groupId]: !prev[groupId] }));

  const [expandedItems, setExpandedItems] = useState<Record<string, boolean>>({});
  const toggleItemExpand = (itemId: string) =>
    setExpandedItems((prev) => ({ ...prev, [itemId]: !prev[itemId] }));

  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: (costId: string) => costTrackerApi.deleteCost(tenantId, projectId, costId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tracker', 'by-estimate', costEstimateId] });
      queryClient.invalidateQueries({ queryKey: ['tracker', 'by-project'] });
      queryClient.invalidateQueries({ queryKey: ['tracker', 'costs'] });
    },
  });

  const confirmDelete = (costId: string, name: string) => {
    if (window.confirm(`Usuń koszt "${name}"?`)) {
      deleteMutation.mutate(costId);
    }
  };

  // Endpoint zwraca teraz CostEstimateSummaryWeb bezpośrednio
  const { data: summary, isLoading, isError } = useQuery({
    queryKey: ['tracker', 'by-estimate', costEstimateId],
    queryFn: () => costTrackerApi.getByEstimate(tenantId, projectId, costEstimateId),
    staleTime: 0,
  });

  if (isLoading) {
    return (
      <div className="p-8 flex justify-center">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" />
      </div>
    );
  }

  if (isError || !summary) {
    return (
      <div className="p-8 text-center text-sm text-gray-400">
        Nie udało się załadować danych trackera.
      </div>
    );
  }

  const invalidateKeys = [
    ['tracker', 'by-estimate', costEstimateId],
    ['tracker', 'by-project'],
    ['tracker', 'costs'],
  ];

  const canAddCosts = Boolean(trackerId);

  // Rekurencyjny renderer grupy i jej dzieci
  const renderGroup = (group: TrackerGroupWeb, depth = 0): React.ReactNode => {
    const isCollapsed = Boolean(collapsed[group.groupId]);
    const groupStatus = worstGroupStatus(group);
    const allItems = group.items;
    const paddingLeft = 16 + depth * 16;

    return (
      <React.Fragment key={group.groupId}>
        {/* Wiersz nagłówka grupy */}
        <tr
          className={`cursor-pointer hover:bg-blue-100 transition-colors ${depth === 0 ? 'bg-blue-50' : 'bg-blue-50/60'}`}
          onClick={() => toggleGroup(group.groupId)}
        >
          <td className="px-4 py-2" style={{ paddingLeft }}>
            <span className="flex items-center gap-1.5 font-medium text-blue-800">
              {isCollapsed
                ? <ChevronRight size={14} className="shrink-0 text-blue-500" />
                : <ChevronDown size={14} className="shrink-0 text-blue-500" />}
              {group.groupName}
              <span className="text-xs text-gray-400 font-normal ml-1">
                ({allItems.filter((i) => i.costsNet != null).length}/{allItems.length})
              </span>
            </span>
          </td>
          <td className="px-4 py-2 text-right font-medium">
            <MoneyCell value={group.budgetNet} currency={currencySymbol} />
          </td>
          <td className="px-4 py-2 text-right font-medium">
            <MoneyCell value={group.costsNet} currency={currencySymbol} />
          </td>
          <td className="px-4 py-2 text-right">
            <DeviationCell deviation={group.deviationNet} percent={group.deviationPercent} />
          </td>
          <td className="px-4 py-2 text-center">
            <ItemStatusBadge status={groupStatus} />
          </td>
          <td className="px-3 py-2" />
        </tr>

        {/* Pozycje grupy */}
        {!isCollapsed && allItems
          .slice()
          .sort((a, b) => (b.deviationNet ?? 0) - (a.deviationNet ?? 0))
          .map((item) => {
            const rowBg =
              item.status === TrackedCostItemStatus.OverBudget
                ? 'bg-red-50'
                : item.status === TrackedCostItemStatus.NearLimit
                  ? 'bg-orange-50'
                  : '';
            const isExpanded = Boolean(expandedItems[item.costEstimateItemId]);
            const inlineCosts = item.costs ?? [];
            const hasCosts = inlineCosts.length > 0;

            return (
              <React.Fragment key={item.costEstimateItemId}>
                <tr className={`hover:bg-gray-50 transition-colors ${rowBg}`}>
                  <td className="px-4 py-2 text-gray-800" style={{ paddingLeft: paddingLeft + 16 }}>{item.name}</td>
                  <td className="px-4 py-2 text-right font-mono text-gray-700">
                    <MoneyCell value={item.budgetNet} currency={currencySymbol} />
                  </td>
                  <td className="px-4 py-2 text-right font-mono text-gray-700">
                    <MoneyCell value={item.costsNet} currency={currencySymbol} />
                  </td>
                  <td className="px-4 py-2 text-right">
                    <DeviationCell deviation={item.deviationNet} percent={item.deviationPercent} />
                  </td>
                  <td className="px-4 py-2 text-center">
                    <ItemStatusBadge status={item.status} />
                  </td>
                  <td className="px-3 py-2 text-center">
                    <div className="flex items-center justify-center gap-0.5">
                      {hasCosts && (
                        <button
                          type="button"
                          title={isExpanded ? 'Ukryj koszty' : `Pokaż ${inlineCosts.length} koszt(y)`}
                          onClick={() => toggleItemExpand(item.costEstimateItemId)}
                          className={`p-1 rounded transition-colors ${
                            isExpanded ? 'text-blue-600 bg-blue-50' : 'text-gray-500 hover:bg-gray-100 hover:text-gray-800'
                          }`}
                        >
                          <List size={13} />
                        </button>
                      )}
                      {canAddCosts && (
                        <button
                          type="button"
                          title="Dodaj koszt"
                          onClick={() =>
                            setAddCostContext({
                              costEstimateId: summary.costEstimateId,
                              costEstimateItemId: item.costEstimateItemId,
                              label: item.name,
                            })
                          }
                          className="p-1 rounded text-blue-600 hover:bg-blue-50 transition-colors"
                        >
                          <Plus size={13} />
                        </button>
                      )}
                    </div>
                  </td>
                </tr>

                {/* Rozwinięta lista kosztów pozycji */}
                {isExpanded && inlineCosts.length > 0 && (
                  <tr>
                    <td
                      colSpan={6}
                      className="p-0 bg-slate-50 border-b border-slate-100"
                    >
                      <div className="px-8 py-2">
                        <table className="w-full text-xs">
                          <thead>
                            <tr className="text-gray-500 border-b border-gray-200">
                              <th className="pb-1.5 text-left font-medium">Nazwa kosztu</th>
                              <th className="pb-1.5 text-left font-medium">Kontrahent</th>
                              <th className="pb-1.5 text-left font-medium">Data</th>
                              <th className="pb-1.5 text-right font-medium">Netto</th>
                              <th className="pb-1.5 text-right font-medium">Brutto</th>
                              {canAddCosts && <th className="pb-1.5 w-12" />}
                            </tr>
                          </thead>
                          <tbody className="divide-y divide-gray-100">
                            {inlineCosts.map((cost) => (
                              <tr key={cost.id} className="hover:bg-slate-100 cursor-pointer" onClick={() => setEditCost(cost)}>
                                <td className="py-1.5 text-gray-800">{cost.name}</td>
                                <td className="py-1.5 text-gray-600">{cost.contractor ?? '—'}</td>
                                <td className="py-1.5 text-gray-600">
                                  {cost.date ? new Date(cost.date).toLocaleDateString('pl-PL') : '—'}
                                </td>
                                <td className="py-1.5 text-right font-mono text-gray-700">
                                  <MoneyCell value={cost.net} currency={currencySymbol} />
                                </td>
                                <td className="py-1.5 text-right font-mono text-gray-700">
                                  <MoneyCell value={cost.gross} currency={currencySymbol} />
                                </td>
                                {canAddCosts && (
                                  <td className="py-1.5 text-right">
                                    <div className="flex items-center justify-end gap-0.5">
                                      <button
                                        type="button"
                                        title="Edytuj"
                                        onClick={(e) => { e.stopPropagation(); setEditCost(cost); }}
                                        className="p-1 rounded text-gray-400 hover:text-blue-600 hover:bg-blue-50 transition-colors"
                                      >
                                        <Pencil size={11} />
                                      </button>
                                      <button
                                        type="button"
                                        title="Usuń"
                                        onClick={(e) => { e.stopPropagation(); confirmDelete(cost.id, cost.name); }}
                                        className="p-1 rounded text-gray-400 hover:text-red-600 hover:bg-red-50 transition-colors"
                                      >
                                        <Trash2 size={11} />
                                      </button>
                                    </div>
                                  </td>
                                )}
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                    </td>
                  </tr>
                )}
              </React.Fragment>
            );
          })}

        {/* Podgrupy (childGroups) */}
        {!isCollapsed && (group.childGroups ?? []).map((child) => renderGroup(child, depth + 1))}
      </React.Fragment>
    );
  };

  return (
    <div className="space-y-6 p-1">
      {/* ------------------------------------------------------------------ */}
      {/* Rząd kart statystycznych                                            */}
      {/* ------------------------------------------------------------------ */}
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        {/* Budżet */}
        <div className="bg-white rounded-xl border border-gray-200 p-4 shadow-sm">
          <p className="text-xs text-gray-500 mb-1">Budżet</p>
          <p className="text-base font-bold text-gray-900">
            <MoneyCell value={summary.totalBudgetNet} currency={currencySymbol} className="text-base font-bold text-gray-900" />
          </p>
          <p className="text-xs text-gray-500 mt-0.5">
            brutto: <MoneyCell value={summary.totalBudgetGross} currency={currencySymbol} className="text-xs text-gray-600" />
          </p>
          <p className="text-xs text-gray-400 mt-1">z kosztorysu</p>
        </div>

        {/* Koszty rzeczywiste */}
        <div className={`bg-white rounded-xl border p-4 shadow-sm ${
          summary.isBudgetExceeded ? 'border-red-300 bg-red-50' : 'border-gray-200'
        }`}>
          <p className="text-xs text-gray-500 mb-1">Koszty rzeczywiste</p>
          <p className="text-base font-bold">
            <MoneyCell
              value={summary.totalCostsNet}
              currency={currencySymbol}
              className={`text-base font-bold ${summary.isBudgetExceeded ? 'text-red-700' : 'text-gray-900'}`}
            />
          </p>
          <p className="text-xs text-gray-500 mt-0.5">
            brutto: <MoneyCell value={summary.totalCostsGross} currency={currencySymbol} className="text-xs text-gray-600" />
          </p>
          {summary.totalDeviationNet !== null && (
            <div className="mt-1">
              <DeviationCell
                deviation={summary.totalDeviationNet}
                percent={summary.totalDeviationPercent}
              />
            </div>
          )}
        </div>

        {/* Wypełnienie */}
        <div className="bg-white rounded-xl border border-gray-200 p-4 shadow-sm">
          <p className="text-xs text-gray-500 mb-1">Wypełnienie pozycji</p>
          <p className="text-xl font-bold text-gray-900">
            {summary.coveredPercent != null
              ? `${Math.round(summary.coveredPercent)}%`
              : '—'}
          </p>
          <div className="mt-2">
            <CoverageMiniBar
              covered={summary.itemsWithCostsCount}
              total={summary.totalItemsCount}
            />
          </div>
        </div>

        {/* Alerty */}
        <div className="bg-white rounded-xl border border-gray-200 p-4 shadow-sm">
          <p className="text-xs text-gray-500 mb-2">Alerty</p>
          <div className="flex items-center gap-3">
            <div className="flex items-center gap-1">
              <AlertTriangle size={14} className="text-red-500" />
              <span className="text-lg font-bold text-red-600">{summary.itemsOverBudgetCount}</span>
              <span className="text-xs text-gray-500">przekr.</span>
            </div>
            <div className="flex items-center gap-1">
              <TrendingUp size={14} className="text-orange-500" />
              <span className="text-lg font-bold text-orange-500">{summary.itemsNearLimitCount}</span>
              <span className="text-xs text-gray-500">blisko</span>
            </div>
          </div>
        </div>
      </div>

      {/* ------------------------------------------------------------------ */}
      {/* Tabela pozycji z odchyleniami                                       */}
      {/* ------------------------------------------------------------------ */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        <div className="px-5 py-3 border-b border-gray-200">
          <h3 className="text-sm font-semibold text-gray-800">Pozycje kosztorysu</h3>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-gray-50 text-left">
                <th className="px-4 py-2.5 text-xs font-semibold text-gray-600">Pozycja</th>
                <th className="px-4 py-2.5 text-xs font-semibold text-gray-600 text-right">Budżet netto</th>
                <th className="px-4 py-2.5 text-xs font-semibold text-gray-600 text-right">Koszty rzecz.</th>
                <th className="px-4 py-2.5 text-xs font-semibold text-gray-600 text-right">Odchylenie</th>
                <th className="px-4 py-2.5 text-xs font-semibold text-gray-600 text-center">Status</th>
                <th className="px-3 py-2.5 text-xs font-semibold text-gray-600 text-center w-16">Koszty</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {(summary.groups ?? []).map((group) => renderGroup(group))}

              {/* Wiersz podsumowania całego kosztorysu */}
              {(summary.groups ?? []).length > 0 && (() => {
                const summaryStatus: TrackedCostItemStatus =
                  summary.itemsOverBudgetCount > 0
                    ? TrackedCostItemStatus.OverBudget
                    : summary.itemsNearLimitCount > 0
                      ? TrackedCostItemStatus.NearLimit
                      : summary.totalCostsNet != null
                        ? TrackedCostItemStatus.InProgress
                        : summary.totalBudgetNet != null
                          ? TrackedCostItemStatus.NoBudget
                          : TrackedCostItemStatus.NoCosts;
                return (
                  <tr className="bg-slate-100 border-t border-slate-300">
                    <td className="px-4 py-2.5 font-semibold text-slate-800">Podsumowanie</td>
                    <td className="px-4 py-2.5 text-right font-semibold">
                      <MoneyCell value={summary.totalBudgetNet} currency={currencySymbol} />
                    </td>
                    <td className="px-4 py-2.5 text-right font-semibold">
                      <MoneyCell value={summary.totalCostsNet} currency={currencySymbol} />
                    </td>
                    <td className="px-4 py-2.5 text-right">
                      <DeviationCell
                        deviation={summary.totalDeviationNet}
                        percent={summary.totalDeviationPercent}
                      />
                    </td>
                    <td className="px-4 py-2.5 text-center">
                      <ItemStatusBadge status={summaryStatus} />
                    </td>
                    <td className="px-3 py-2.5" />
                  </tr>
                );
              })()}
            </tbody>
          </table>
        </div>
      </div>

      {/* ------------------------------------------------------------------ */}
      {/* Koszty dodatkowe kosztorysu                                        */}
      {/* ------------------------------------------------------------------ */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        <div className="flex items-center justify-between px-5 py-3 border-b border-gray-200">
          <div>
            <h3 className="text-sm font-semibold text-gray-800">Koszty dodatkowe kosztorysu</h3>
            <p className="text-xs text-gray-500 mt-0.5">
              Koszty powiązane z kosztorysem, ale bez konkretnej pozycji
            </p>
          </div>
          {canAddCosts && (
            <button
              type="button"
              onClick={() =>
                setAddCostContext({
                  costEstimateId: summary.costEstimateId,
                  label: 'Koszt dodatkowy kosztorysu',
                })
              }
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-blue-700 bg-blue-50 border border-blue-200 rounded-lg hover:bg-blue-100 transition-colors"
            >
              <Plus size={13} />
              Dodaj koszt dodatkowy
            </button>
          )}
        </div>

        {(summary.additionalCosts?.costs ?? []).length === 0 ? (
          <div className="px-5 py-6 text-center text-sm text-gray-400">
            Brak kosztów dodatkowych
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-gray-50 text-left">
                    <th className="px-4 py-2 text-xs font-semibold text-gray-600">Nazwa</th>
                    <th className="px-4 py-2 text-xs font-semibold text-gray-600">Kontrahent</th>
                    <th className="px-4 py-2 text-xs font-semibold text-gray-600">Data</th>
                    <th className="px-4 py-2 text-xs font-semibold text-gray-600 text-right">Netto</th>
                    <th className="px-4 py-2 text-xs font-semibold text-gray-600 text-right">Brutto</th>
                    {canAddCosts && <th className="px-4 py-2 w-16" />}
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {(summary.additionalCosts?.costs ?? []).map((cost) => (
                    <tr key={cost.id} className="hover:bg-gray-50 cursor-pointer" onClick={() => setEditCost(cost)}>
                      <td className="px-4 py-2 text-gray-800">{cost.name}</td>
                      <td className="px-4 py-2 text-gray-600">{cost.contractor ?? '—'}</td>
                      <td className="px-4 py-2 text-gray-600">
                        {cost.date ? new Date(cost.date).toLocaleDateString('pl-PL') : '—'}
                      </td>
                      <td className="px-4 py-2 text-right font-mono">
                        <MoneyCell value={cost.net} currency={currencySymbol} />
                      </td>
                      <td className="px-4 py-2 text-right font-mono">
                        <MoneyCell value={cost.gross} currency={currencySymbol} />
                      </td>
                      {canAddCosts && (
                        <td className="px-4 py-2 text-right">
                          <div className="flex items-center justify-end gap-0.5">
                            <button
                              type="button"
                              title="Edytuj"
                              onClick={(e) => { e.stopPropagation(); setEditCost(cost); }}
                              className="p-1 rounded text-gray-400 hover:text-blue-600 hover:bg-blue-50 transition-colors"
                            >
                              <Pencil size={13} />
                            </button>
                            <button
                              type="button"
                              title="Usuń"
                              onClick={(e) => { e.stopPropagation(); confirmDelete(cost.id, cost.name); }}
                              className="p-1 rounded text-gray-400 hover:text-red-600 hover:bg-red-50 transition-colors"
                            >
                              <Trash2 size={13} />
                            </button>
                          </div>
                        </td>
                      )}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div className="px-4 py-2.5 bg-gray-50 border-t border-gray-100 flex justify-end gap-6 text-xs text-gray-600">
              <span>
                Suma netto:{' '}
                <strong>
                  <MoneyCell value={summary.additionalCosts?.totalNet ?? null} currency={currencySymbol} />
                </strong>
              </span>
              <span>
                Suma brutto:{' '}
                <strong>
                  <MoneyCell value={summary.additionalCosts?.totalGross ?? null} currency={currencySymbol} />
                </strong>
              </span>
            </div>
          </>
        )}
      </div>

      {/* Modal dodawania kosztu */}
      {addCostContext && trackerId && (
        <TrackedCostModal
          isOpen={Boolean(addCostContext)}
          onClose={() => setAddCostContext(null)}
          tenantId={tenantId}
          projectId={projectId}
          trackerId={trackerId}
          costEstimateId={addCostContext.costEstimateId}
          costEstimateItemId={addCostContext.costEstimateItemId}
          contextLabel={addCostContext.label}
          invalidateKeys={invalidateKeys}
        />
      )}

      {/* Modal edycji / podglądu kosztu */}
      {editCost && trackerId && (
        <TrackedCostModal
          isOpen={Boolean(editCost)}
          onClose={() => setEditCost(null)}
          tenantId={tenantId}
          projectId={projectId}
          trackerId={trackerId}
          editCost={editCost}
          invalidateKeys={invalidateKeys}
        />
      )}

      {/* ------------------------------------------------------------------ */}
      {/* Łączne koszty kosztorysu (pozycje + dodatkowe)                     */}
      {/* ------------------------------------------------------------------ */}
      {(summary.totalCostsNet != null || summary.totalCostsGross != null ||
        summary.additionalCostsNet != null || summary.additionalCostsGross != null) && (
        <div className="bg-blue-50 rounded-xl border border-blue-200 p-4 shadow-sm">
          <h3 className="text-sm font-semibold text-blue-800 mb-3">Łączne koszty kosztorysu</h3>
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
            <div>
              <p className="text-xs text-blue-600">Koszty pozycji netto</p>
              <p className="text-base font-bold text-blue-900 mt-0.5">
                <MoneyCell value={summary.totalCostsNet} currency={currencySymbol} className="text-base font-bold text-blue-900" />
              </p>
            </div>
            <div>
              <p className="text-xs text-blue-600">Koszty pozycji brutto</p>
              <p className="text-base font-bold text-blue-900 mt-0.5">
                <MoneyCell value={summary.totalCostsGross} currency={currencySymbol} className="text-base font-bold text-blue-900" />
              </p>
            </div>
            <div>
              <p className="text-xs text-blue-600">Koszty dodatkowe netto</p>
              <p className="text-base font-bold text-blue-900 mt-0.5">
                <MoneyCell value={summary.additionalCostsNet} currency={currencySymbol} className="text-base font-bold text-blue-900" />
              </p>
            </div>
            <div>
              <p className="text-xs text-blue-600">Koszty dodatkowe brutto</p>
              <p className="text-base font-bold text-blue-900 mt-0.5">
                <MoneyCell value={summary.additionalCostsGross} currency={currencySymbol} className="text-base font-bold text-blue-900" />
              </p>
            </div>
          </div>
          {summary.additionalCostsCount > 0 && (
            <p className="text-xs text-blue-500 mt-2">
              Koszty dodatkowe: {summary.additionalCostsCount} {summary.additionalCostsCount === 1 ? 'wpis' : 'wpisów'}
            </p>
          )}
        </div>
      )}
    </div>
  );
};
