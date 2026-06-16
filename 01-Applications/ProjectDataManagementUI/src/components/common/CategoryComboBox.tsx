/**
 * Stub komponentu CategoryComboBox.
 * @deprecated Używany przez deprecated CostEstimateTableView.
 */

import React from 'react';
import { Select } from '@chakra-ui/react';

interface CategoryComboBoxProps {
  value?: string | null;
  onChange?: (value: string | undefined) => void;
  isDisabled?: boolean;
  categories?: Array<{ id: string; name: string; symbol?: string | null }>;
  [key: string]: unknown;
}

export const CategoryComboBox: React.FC<CategoryComboBoxProps> = ({
  value,
  onChange,
  isDisabled,
  categories = [],
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
      {categories.map((c) => (
        <option key={c.id} value={c.id}>
          {c.name}
        </option>
      ))}
    </Select>
  );
};

export default CategoryComboBox;
