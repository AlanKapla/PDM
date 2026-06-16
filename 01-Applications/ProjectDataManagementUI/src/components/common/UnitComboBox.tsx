/**
 * Stub komponentu UnitComboBox.
 * @deprecated Używany przez deprecated CostEstimateTableView.
 */

import React from 'react';
import { Select } from '@chakra-ui/react';

interface UnitComboBoxProps {
  value?: string | null;
  onChange?: (value: string | undefined) => void;
  isDisabled?: boolean;
  units?: Array<{ id: string; code: string; symbol: string }>;
  [key: string]: unknown;
}

export const UnitComboBox: React.FC<UnitComboBoxProps> = ({
  value,
  onChange,
  isDisabled,
  units = [],
  ...rest
}) => {
  return (
    <Select
      value={value ?? ''}
      onChange={(e) => onChange?.(e.target.value || undefined)}
      isDisabled={isDisabled}
      size="sm"
      {...rest}
    >
      <option value="">—</option>
      {units.map((u) => (
        <option key={u.id} value={u.code}>
          {u.symbol}
        </option>
      ))}
    </Select>
  );
};

export default UnitComboBox;
