import React from 'react';
import { Table, Thead, Tbody, Tr, Th, Td, IconButton, useToken } from '@chakra-ui/react';
import { Pencil, Trash2 } from 'lucide-react';
import type { TrackedCostWeb } from '../../types/projectDashboard.types';
import { DATE } from '../../utils/formatters';
import { AttachmentsCell } from './AttachmentsCell';
import { NetGrossAmount } from './NetGrossAmount';

export interface CostTableProps {
  costs: TrackedCostWeb[];
  title?: string;
  bgOverride?: string;
  onEdit?: (cost: TrackedCostWeb) => void;
  onDelete?: (cost: TrackedCostWeb) => void;
}

/** Tabela kosztów śledzonych. Wartości null wyświetlane jako "—". */
export function CostTable({ costs, title, bgOverride, onEdit, onDelete }: CostTableProps): React.ReactElement {
  const [neutral400, neutral600] = useToken('colors', ['neutral.400', 'neutral.600']);

  const hasActions = onEdit != null || onDelete != null;

  return (
    <div style={{ background: bgOverride ?? '#fff' }}>
      {title && (
        <div style={{ fontSize: "xs", fontWeight: "semibold", color: neutral600, marginBottom: 6 }}>
          {title}
        </div>
      )}
      {costs.length === 0 ? (
        <div style={{ fontSize: "xs", color: neutral400, fontStyle: 'italic' }}>
          Brak kosztów
        </div>
      ) : (
        <div className="dashboard-table-wrap">
        <Table size="sm" variant="simple">
          <Thead>
            <Tr>
              {['Nazwa', 'Wykonawca', 'Data', 'Nr faktury', 'Kwota', 'Zał.'].map((col) => (
                <Th
                  key={col}
                  isNumeric={col === 'Kwota'}
                  color="neutral.400"
                  fontWeight="medium"
                  px="6px"
                  py="4px"
                  borderBottomWidth="0.5px"
                  borderBottomColor="neutral.200"
                  display={col === 'Data' || col === 'Nr faktury' ? { base: 'none', md: 'table-cell' } : undefined}
                >
                  {col}
                </Th>
              ))}
              {hasActions && (
                <Th
                  px="6px"
                  py="4px"
                  borderBottomWidth="0.5px"
                  borderBottomColor="neutral.200"
                  w="80px"
                />
              )}
            </Tr>
          </Thead>
          <Tbody>
            {costs.map((cost) => (
              <Tr key={cost.id}>
                <Td px="6px" py="4px">{cost.name}</Td>
                <Td px="6px" py="4px" color="neutral.600">
                  {cost.contractorName ?? '—'}
                </Td>
                <Td px="6px" py="4px" color="neutral.600" display={{ base: 'none', md: 'table-cell' }}>
                  {DATE(cost.date)}
                </Td>
                <Td px="6px" py="4px" color="neutral.600" display={{ base: 'none', md: 'table-cell' }}>
                  {cost.number ?? '—'}
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
                <Td px="6px" py="4px">
                  <AttachmentsCell attachments={cost.attachments} costName={cost.name} />
                </Td>
                {hasActions && (
                  <Td px="6px" py="4px" whiteSpace="nowrap">
                    {onEdit && (
                      <IconButton
                        size="xs"
                        variant="ghost"
                        colorScheme="gray"
                        aria-label="Edytuj"
                        icon={<Pencil size={12} />}
                        onClick={() => onEdit(cost)}
                        mr={1}
                      />
                    )}
                    {onDelete && (
                      <IconButton
                        size="xs"
                        variant="ghost"
                        colorScheme="red"
                        aria-label="Usuń"
                        icon={<Trash2 size={12} />}
                        onClick={() => onDelete(cost)}
                      />
                    )}
                  </Td>
                )}
              </Tr>
            ))}
          </Tbody>
        </Table>
        </div>
      )}
    </div>
  );
}

export default CostTable;
