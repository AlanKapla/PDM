import React from 'react';
import type { TrackedCostWeb } from '../../types/projectDashboard.types';
import { PLN, DATE } from '../../utils/formatters';
import { COLOR_PALETTE } from '../../utils/colors';
import { AttachmentsCell } from './AttachmentsCell';

export interface CostTableProps {
  costs: TrackedCostWeb[];
  title?: string;
  bgOverride?: string;
  onEdit?: (cost: TrackedCostWeb) => void;
  onDelete?: (cost: TrackedCostWeb) => void;
}

/** Tabela kosztów śledzonych. Wartości null wyświetlane jako "—". */
export function CostTable({ costs, title, bgOverride, onEdit, onDelete }: CostTableProps): React.ReactElement {
  const hasActions = onEdit != null || onDelete != null;

  return (
    <div style={{ background: bgOverride ?? '#fff' }}>
      {title && (
        <div style={{ fontSize: 11, fontWeight: 600, color: COLOR_PALETTE.gray600, marginBottom: 6 }}>
          {title}
        </div>
      )}
      {costs.length === 0 ? (
        <div style={{ fontSize: 11, color: COLOR_PALETTE.gray400, fontStyle: 'italic' }}>
          Brak kosztów
        </div>
      ) : (
        <div className="dashboard-table-wrap">
        <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 11 }}>
          <thead>
            <tr>
              {['Nazwa', 'Wykonawca', 'Data', 'Nr faktury', 'Kwota netto', 'Zał.'].map((col) => (
                <th
                  key={col}
                  style={{
                    textAlign: 'left',
                    padding: '4px 6px',
                    color: COLOR_PALETTE.gray400,
                    fontWeight: 500,
                    borderBottom: `0.5px solid ${COLOR_PALETTE.border}`,
                  }}
                >
                  {col}
                </th>
              ))}
              {hasActions && (
                <th
                  style={{
                    padding: '4px 6px',
                    borderBottom: `0.5px solid ${COLOR_PALETTE.border}`,
                    width: 80,
                  }}
                />
              )}
            </tr>
          </thead>
          <tbody>
            {costs.map((cost) => (
              <tr key={cost.id}>
                <td style={{ padding: '4px 6px' }}>{cost.name}</td>
                <td style={{ padding: '4px 6px', color: COLOR_PALETTE.gray600 }}>
                  {cost.contractor ?? '—'}
                </td>
                <td style={{ padding: '4px 6px', color: COLOR_PALETTE.gray600 }}>
                  {DATE(cost.date)}
                </td>
                <td style={{ padding: '4px 6px', color: COLOR_PALETTE.gray600 }}>
                  {cost.number ?? '—'}
                </td>
                <td style={{ padding: '4px 6px', color: COLOR_PALETTE.coral400, fontWeight: 500 }}>
                  {PLN(cost.net)}
                </td>
                <td style={{ padding: '4px 6px' }}>
                  <AttachmentsCell attachments={cost.attachments} costName={cost.name} />
                </td>
                {hasActions && (
                  <td style={{ padding: '4px 6px', whiteSpace: 'nowrap' }}>
                    {onEdit && (
                      <button
                        onClick={() => onEdit(cost)}
                        style={{
                          fontSize: 11,
                          padding: '4px 10px',
                          background: COLOR_PALETTE.gray50,
                          color: COLOR_PALETTE.gray600,
                          border: `0.5px solid ${COLOR_PALETTE.border}`,
                          borderRadius: 4,
                          cursor: 'pointer',
                          marginRight: 4,
                        }}
                      >
                        Edytuj
                      </button>
                    )}
                    {onDelete && (
                      <button
                        onClick={() => onDelete(cost)}
                        style={{
                          fontSize: 11,
                          padding: '4px 10px',
                          background: COLOR_PALETTE.red50,
                          color: COLOR_PALETTE.red600,
                          border: `0.5px solid ${COLOR_PALETTE.red400}`,
                          borderRadius: 4,
                          cursor: 'pointer',
                        }}
                      >
                        Usuń
                      </button>
                    )}
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
        </div>
      )}
    </div>
  );
}

export default CostTable;
