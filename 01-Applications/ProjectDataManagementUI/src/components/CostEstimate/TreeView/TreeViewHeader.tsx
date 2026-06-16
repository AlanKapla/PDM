/**
 * Tree View Header — Column headers with sort icons and search input
 *
 * Layout:
 *  [Search input]  [Name (flex)]  [base cols (no-name)]  [additional cols]  [Akcje]
 *
 * Sort: clicking a sortable column header toggles asc → desc → off
 */

import React, { useState, useRef, useCallback, useEffect } from 'react';
import {
  Flex,
  Text,
  Box,
  IconButton,
  Input,
  InputGroup,
  InputLeftElement,
  Tooltip,
} from '@chakra-ui/react';
import { ArrowUp, ArrowDown, ArrowUpDown, Search, X } from 'lucide-react';
import type { ColumnDef, SortConfig } from './CostEstimateTreeView';

export const HEADER_COLUMN_BORDER = {
  borderRight: '1px solid',
  borderColor: 'neutral.200',
} as const;

// ---------------------------------------------------------------------------
// Debounced search input
// ---------------------------------------------------------------------------

export interface SearchInputProps {
  value: string;
  onChange: (q: string) => void;
}

export const SearchInput: React.FC<SearchInputProps> = ({ value, onChange }) => {
  const [local, setLocal] = useState(value);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    if (value === '') setLocal('');
  }, [value]);

  const handleChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      const v = e.target.value;
      setLocal(v);
      if (timerRef.current) clearTimeout(timerRef.current);
      timerRef.current = setTimeout(() => onChange(v), 300);
    },
    [onChange]
  );

  const handleClear = useCallback(() => {
    setLocal('');
    if (timerRef.current) clearTimeout(timerRef.current);
    onChange('');
  }, [onChange]);

  return (
    <InputGroup size="sm" w="220px" flexShrink={0}>
      <InputLeftElement pointerEvents="none">
        <Search size={13} color="var(--chakra-colors-neutral-400)" aria-hidden="true" />
      </InputLeftElement>
      <Input
        value={local}
        onChange={handleChange}
        placeholder="Szukaj w kosztorysie..."
        borderRadius="8px"
        fontSize="xs"
        bg="white"
        borderColor="neutral.200"
        _focus={{ borderColor: 'primary.400', boxShadow: '0 0 0 2px rgba(47,108,236,0.12)' }}
        pr={local ? '28px' : undefined}
        aria-label="Szukaj w kosztorysie"
      />
      {local && (
        <Box
          position="absolute"
          right="6px"
          top="50%"
          transform="translateY(-50%)"
          zIndex={1}
        >
          <IconButton
            aria-label="Wyczyść wyszukiwanie"
            icon={<X size={11} />}
            size="xs"
            variant="ghost"
            colorScheme="gray"
            minW="20px"
            h="20px"
            onClick={handleClear}
          />
        </Box>
      )}
    </InputGroup>
  );
};

// ---------------------------------------------------------------------------
// Sort icon helper
// ---------------------------------------------------------------------------

const SortIcon: React.FC<{ field: string; sortConfig: SortConfig | null }> = ({
  field,
  sortConfig,
}) => {
  if (!sortConfig || sortConfig.field !== field) {
    return <ArrowUpDown size={11} color="var(--chakra-colors-neutral-400)" aria-hidden="true" />;
  }
  if (sortConfig.direction === 'asc') {
    return <ArrowUp size={11} color="var(--chakra-colors-primary-500)" aria-hidden="true" />;
  }
  return <ArrowDown size={11} color="var(--chakra-colors-primary-500)" aria-hidden="true" />;
};

// ---------------------------------------------------------------------------
// Column resize handle
// ---------------------------------------------------------------------------

interface ColumnResizeHandleProps {
  onMouseDown: (e: React.MouseEvent) => void;
}

const ColumnResizeHandle: React.FC<ColumnResizeHandleProps> = ({ onMouseDown }) => (
  <Box
    position="absolute"
    right={0}
    top={0}
    bottom={0}
    w="5px"
    cursor="col-resize"
    onMouseDown={onMouseDown}
    borderRight="2px solid"
    borderColor="neutral.300"
    _hover={{ borderColor: 'primary.400', bg: 'rgba(47, 108, 236, 0.08)' }}
    zIndex={2}
    aria-hidden="true"
  />
);

// ---------------------------------------------------------------------------
// Single column header cell
// ---------------------------------------------------------------------------

interface ColumnHeaderCellProps {
  col: ColumnDef;
  sortConfig: SortConfig | null;
  onSort: (field: string) => void;
  width?: string;
  onResize?: (colId: string, width: number) => void;
}

const ColumnHeaderCell: React.FC<ColumnHeaderCellProps> = ({
  col,
  sortConfig,
  onSort,
  width,
  onResize,
}) => {
  const isActive = sortConfig?.field === col.id;
  const w = width ?? col.width ?? '100px';
  const align = col.textAlign ?? 'right';
  const isDraggingRef = useRef(false);
  const startXRef = useRef(0);
  const startWidthRef = useRef(0);

  const handleResizeMouseDown = useCallback((e: React.MouseEvent) => {
    if (!onResize) return;
    e.preventDefault();
    e.stopPropagation();
    isDraggingRef.current = true;
    startXRef.current = e.clientX;
    startWidthRef.current = parseInt(w, 10);

    const onMouseMove = (ev: MouseEvent) => {
      if (!isDraggingRef.current) return;
      const delta = ev.clientX - startXRef.current;
      onResize(col.id, startWidthRef.current + delta);
    };
    const onMouseUp = () => {
      isDraggingRef.current = false;
      document.removeEventListener('mousemove', onMouseMove);
      document.removeEventListener('mouseup', onMouseUp);
    };
    document.addEventListener('mousemove', onMouseMove);
    document.addEventListener('mouseup', onMouseUp);
  }, [onResize, col.id, w]);

  const tooltipLabel = col.description ?? col.label;
  const justify =
    align === 'right' ? 'flex-end' : align === 'center' ? 'center' : 'flex-start';

  const headerContent = (
    <Tooltip label={tooltipLabel} placement="top" hasArrow openDelay={500} fontSize="xs">
      {col.isSortable ? (
        <Flex
          as="button"
          align="center"
          gap="4px"
          px={2}
          h="42px"
          cursor="pointer"
          onClick={() => onSort(col.id)}
          _hover={{ color: 'primary.600' }}
          color={isActive ? 'primary.600' : 'neutral.500'}
          justify={justify}
          w="full"
          overflow="hidden"
          aria-sort={isActive ? (sortConfig?.direction === 'asc' ? 'ascending' : 'descending') : 'none'}
          role="columnheader"
        >
          {align === 'right' && <SortIcon field={col.id} sortConfig={sortConfig} />}
          <Text
            fontSize="xs"
            fontWeight="bold"
            textTransform="uppercase"
            letterSpacing="0.045em"
            noOfLines={2}
            userSelect="none"
            lineHeight="1.2"
            whiteSpace="pre-wrap"
            textAlign={align}
          >
            {col.label}
          </Text>
          {align !== 'right' && <SortIcon field={col.id} sortConfig={sortConfig} />}
        </Flex>
      ) : (
        <Flex
          px={2}
          h="42px"
          align="center"
          justify={justify}
          overflow="hidden"
          role="columnheader"
        >
          <Text
            fontSize="xs"
            fontWeight="bold"
            color="neutral.500"
            textTransform="uppercase"
            letterSpacing="0.045em"
            noOfLines={2}
            lineHeight="1.2"
            whiteSpace="pre-wrap"
            textAlign={align}
            userSelect="none"
          >
            {col.label}
          </Text>
        </Flex>
      )}
    </Tooltip>
  );

  return (
    <Box
      position="relative"
      flex="0 0 auto"
      w={w}
      h="42px"
      userSelect="none"
      {...HEADER_COLUMN_BORDER}
    >
      {headerContent}
      {onResize && <ColumnResizeHandle onMouseDown={handleResizeMouseDown} />}
    </Box>
  );
};

// ---------------------------------------------------------------------------
// Props
// ---------------------------------------------------------------------------

export interface TreeViewHeaderProps {
  baseColumns: ColumnDef[];
  additionalColumns: ColumnDef[];
  sortConfig: SortConfig | null;
  onSort: (field: string) => void;
  totalColumnsWidth?: number;
  onResizeColumn?: (colId: string, width: number) => void;
  nameColWidth?: number;
  actionsColWidth?: number;
}

/** Wysokość sticky nagłówka kolumn (pt 1.5 + wiersz 42px + border 1px) — dla wiersza „Razem”. */
export const TREE_VIEW_HEADER_HEIGHT = 49;

// ---------------------------------------------------------------------------
// Component
// ---------------------------------------------------------------------------

export const TreeViewHeader: React.FC<TreeViewHeaderProps> = ({
  baseColumns,
  additionalColumns,
  sortConfig,
  onSort,
  totalColumnsWidth,
  onResizeColumn,
  nameColWidth,
  actionsColWidth,
}) => {
  const actionsResizeRef = useRef(false);
  const actionsStartXRef = useRef(0);
  const actionsStartWidthRef = useRef(0);

  const handleActionsResizeMouseDown = useCallback((e: React.MouseEvent) => {
    if (!onResizeColumn) return;
    e.preventDefault();
    e.stopPropagation();
    actionsResizeRef.current = true;
    actionsStartXRef.current = e.clientX;
    actionsStartWidthRef.current = actionsColWidth ?? 120;

    const onMouseMove = (ev: MouseEvent) => {
      if (!actionsResizeRef.current) return;
      const delta = ev.clientX - actionsStartXRef.current;
      onResizeColumn('actions', actionsStartWidthRef.current + delta);
    };
    const onMouseUp = () => {
      actionsResizeRef.current = false;
      document.removeEventListener('mousemove', onMouseMove);
      document.removeEventListener('mouseup', onMouseUp);
    };
    document.addEventListener('mousemove', onMouseMove);
    document.addEventListener('mouseup', onMouseUp);
  }, [onResizeColumn, actionsColWidth]);

  const explicitBaseColumns = baseColumns.filter((c) => c.id !== 'name' && c.id !== 'actions');
  const nameColumn = baseColumns.find((c) => c.id === 'name');

  return (
    <Flex
      direction="column"
      bg="neutral.50"
      borderBottom="1px solid"
      borderColor="neutral.200"
      position="sticky"
      top={0}
      zIndex={10}
      minW={totalColumnsWidth ? `${totalColumnsWidth}px` : undefined}
    >
      <Box pt={1.5} />

      <Flex
        align="center"
        h="42px"
        px={3.5}
        role="row"
      >
        {nameColumn && (
          <Box
            flex="0 0 auto"
            w={`${nameColWidth}px`}
            h="42px"
            position="sticky"
            left={0}
            zIndex={11}
            bg="neutral.50"
          >
            <ColumnHeaderCell
              col={nameColumn}
              sortConfig={sortConfig}
              onSort={onSort}
              width={`${nameColWidth}px`}
              onResize={onResizeColumn}
            />
          </Box>
        )}

        <Box
          flex="0 0 auto"
          w={`${actionsColWidth}px`}
          h="42px"
          position="sticky"
          left={`${nameColWidth}px`}
          zIndex={11}
          bg="neutral.50"
          userSelect="none"
          {...HEADER_COLUMN_BORDER}
        >
          <Box position="relative" w="full" h="full">
            <Flex align="center" justify="center" h="42px" px={2}>
              <Text
                fontSize="xs"
                fontWeight="bold"
                color="neutral.500"
                textTransform="uppercase"
                letterSpacing="0.045em"
                userSelect="none"
                textAlign="center"
              >
                Akcje
              </Text>
            </Flex>
            {onResizeColumn && <ColumnResizeHandle onMouseDown={handleActionsResizeMouseDown} />}
          </Box>
        </Box>

        {explicitBaseColumns.map((col) => (
          <ColumnHeaderCell
            key={col.id}
            col={col}
            sortConfig={sortConfig}
            onSort={onSort}
            width={`${parseInt(col.width ?? '100', 10)}px`}
            onResize={onResizeColumn}
          />
        ))}

        {additionalColumns.map((col) => (
          <ColumnHeaderCell
            key={col.id}
            col={col}
            sortConfig={sortConfig}
            onSort={onSort}
            width={`${parseInt(col.width ?? '130', 10)}px`}
            onResize={onResizeColumn}
          />
        ))}
      </Flex>
    </Flex>
  );
};
