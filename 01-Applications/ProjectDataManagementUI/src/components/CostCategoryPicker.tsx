import React, { useMemo, useState } from 'react';
import {
  Box,
  HStack,
  IconButton,
  Select,
  Spinner,
  Text,
  Tooltip,
} from '@chakra-ui/react';
import { Plus } from 'lucide-react';
import { useProjectCostCategories } from '../hooks/useProjectCostCategories';
import { CostCategoryQuickAddModal } from './CostCategoryQuickAddModal';
import type { ProjectCostCategoryDto } from '../api/projectApi';

export interface CostCategoryPickerProps {
  tenantId: string;
  projectId: string;
  value: string | null;
  onChange: (id: string | null) => void;
  canQuickAdd?: boolean;
  isDisabled?: boolean;
  isInvalid?: boolean;
  placeholder?: string;
}

function CategoryOptionLabel({ category }: { category: ProjectCostCategoryDto }): React.ReactElement {
  return (
    <HStack spacing={2}>
      {category.color && (
        <Box
          w="10px"
          h="10px"
          borderRadius="sm"
          bg={category.color}
          flexShrink={0}
          aria-hidden="true"
        />
      )}
      <Text as="span">{category.name}</Text>
      {category.code && (
        <Text as="span" fontSize="xs" color="neutral.600">
          ({category.code})
        </Text>
      )}
    </HStack>
  );
}

export function CostCategoryPicker({
  tenantId,
  projectId,
  value,
  onChange,
  canQuickAdd = false,
  isDisabled = false,
  isInvalid = false,
  placeholder = 'Wybierz kategorię (opcjonalnie)',
}: CostCategoryPickerProps): React.ReactElement {
  const { data: categories = [], isLoading } = useProjectCostCategories(tenantId, projectId);
  const [isQuickAddOpen, setIsQuickAddOpen] = useState(false);

  const selectedCategory = useMemo<ProjectCostCategoryDto | null>(
    () => categories.find((c) => c.id === value) ?? null,
    [categories, value]
  );

  if (isLoading) {
    return (
      <HStack spacing={2}>
        <Spinner size="sm" color="primary.600" />
        <Text fontSize="sm" color="neutral.600">
          Ładowanie kategorii…
        </Text>
      </HStack>
    );
  }

  return (
    <>
      <HStack align="flex-start">
        <Box flex={1}>
          <Select
            value={value ?? ''}
            onChange={(e) => onChange(e.target.value || null)}
            isDisabled={isDisabled}
            isInvalid={isInvalid}
            placeholder={placeholder}
            aria-label="Kategoria kosztu"
          >
            {categories.map((category) => (
              <option key={category.id} value={category.id}>
                {category.code ? `${category.name} (${category.code})` : category.name}
              </option>
            ))}
          </Select>
          {selectedCategory && (
            <Box mt={1}>
              <CategoryOptionLabel category={selectedCategory} />
            </Box>
          )}
        </Box>

        {canQuickAdd && (
          <Tooltip label="Dodaj nową kategorię">
            <IconButton
              aria-label="Dodaj nową kategorię"
              icon={<Plus size={16} aria-hidden="true" />}
              size="md"
              variant="outline"
              onClick={() => setIsQuickAddOpen(true)}
              isDisabled={isDisabled}
              flexShrink={0}
            />
          </Tooltip>
        )}
      </HStack>

      {canQuickAdd && (
        <CostCategoryQuickAddModal
          isOpen={isQuickAddOpen}
          onClose={() => setIsQuickAddOpen(false)}
          tenantId={tenantId}
          projectId={projectId}
          onCreated={(id) => {
            onChange(id);
            setIsQuickAddOpen(false);
          }}
        />
      )}
    </>
  );
}

export default CostCategoryPicker;
