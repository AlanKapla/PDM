/**
 * Prototype-styled action buttons
 * Ghost buttons that appear on row hover
 */

import React from 'react';
import { IconButton, type IconButtonProps, Tooltip, Button } from '@chakra-ui/react';
import { Plus, Trash2, GripVertical, Eye, EyeOff, Edit2 } from 'lucide-react';

interface GhostActionButtonProps extends Omit<IconButtonProps, 'aria-label'> {
  label: string;
  icon: React.ReactElement;
  variant?: 'default' | 'add' | 'delete';
  /** W TreeView — bez tła przy hover */
  blendWithRow?: boolean;
}

/**
 * Ghost action button - always visible
 * Uses Chakra color tokens for consistency
 */
export const GhostActionButton: React.FC<GhostActionButtonProps> = ({
  label,
  icon,
  variant = 'default',
  isDisabled,
  blendWithRow = false,
  ...props
}) => {
  const buttonOpacity = isDisabled ? 0.4 : 1;

  return (
    <Tooltip label={label} placement="top" hasArrow>
      <IconButton
        aria-label={label}
        icon={icon}
        isDisabled={isDisabled}
        size="sm"
        variant="ghost"
        color="gray.400"
        opacity={buttonOpacity}
        transition="color 0.12s"
        borderRadius="8px"
        w="32px"
        h="32px"
        minW="32px"
        _hover={
          blendWithRow
            ? { bg: 'transparent', color: 'gray.600' }
            : {}
        }
        _active={blendWithRow ? { bg: 'transparent' } : undefined}
        {...props}
      />
    </Tooltip>
  );
};

/**
 * Add inline button (always visible)
 * Uses Chakra Button with primary color scheme
 */
export const AddInlineButton: React.FC<{
  onClick: () => void;
  children: React.ReactNode;
}> = ({ onClick, children }) => {
  return (
    <Button
      onClick={onClick}
      variant="ghost"
      colorScheme="primary"
      leftIcon={<Plus size={15} />}
      size="sm"
      fontWeight="semibold"
      fontSize="sm"
      px="10px"
      py="7px"
      borderRadius="8px"
      flexShrink={0}
      whiteSpace="nowrap"
      _hover={{ bg: 'primary.50' }}
    >
      {children}
    </Button>
  );
};

/**
 * Drag handle icon (grip)
 * Uses Chakra color tokens, 44x44px for touch targets
 */
export const DragHandle: React.FC<{ isDragging?: boolean; blendWithRow?: boolean }> = ({
  isDragging,
  blendWithRow = false,
}) => {
  return (
    <div
      style={{
        width: '44px',
        height: '44px',
        borderRadius: '8px',
        display: 'grid',
        placeItems: 'center',
        color: 'var(--chakra-colors-gray-500)',
        cursor: isDragging ? 'grabbing' : 'grab',
        transition: 'color 0.12s',
      }}
      onMouseEnter={(e) => {
        if (!isDragging && !blendWithRow) {
          e.currentTarget.style.background = 'var(--chakra-colors-gray-100)';
          e.currentTarget.style.color = 'var(--chakra-colors-gray-800)';
        }
      }}
      onMouseLeave={(e) => {
        if (!blendWithRow) {
          e.currentTarget.style.background = 'transparent';
        }
        e.currentTarget.style.color = 'var(--chakra-colors-gray-500)';
      }}
    >
      <GripVertical size={16} />
    </div>
  );
};

/**
 * Chevron button for expand/collapse
 */
export const ChevronButton: React.FC<{
  isExpanded: boolean;
  onClick: () => void;
  isLeaf?: boolean;
  blendWithRow?: boolean;
}> = ({ isExpanded, onClick, isLeaf, blendWithRow = false }) => {
  return (
    <button
      onClick={onClick}
      aria-label={isExpanded ? 'Zwiń' : 'Rozwiń'}
      style={{
        width: '32px',
        height: '32px',
        borderRadius: '8px',
        display: 'grid',
        placeItems: 'center',
        color: 'var(--chakra-colors-gray-500)',
        cursor: isLeaf ? 'default' : 'pointer',
        visibility: isLeaf ? 'hidden' : 'visible',
        background: 'transparent',
        border: 'none',
        transition: 'color 0.12s',
      }}
      onMouseEnter={(e) => {
        if (!isLeaf && !blendWithRow) {
          e.currentTarget.style.background = 'var(--chakra-colors-gray-100)';
          e.currentTarget.style.color = 'var(--chakra-colors-gray-800)';
        }
      }}
      onMouseLeave={(e) => {
        if (!blendWithRow) {
          e.currentTarget.style.background = 'transparent';
        }
        e.currentTarget.style.color = 'var(--chakra-colors-gray-500)';
      }}
    >
      <svg
        width="15"
        height="15"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
        style={{
          transform: isExpanded ? 'rotate(0deg)' : 'rotate(-90deg)',
          transition: 'transform 0.15s',
        }}
      >
        <polyline points="6 9 12 15 18 9" />
      </svg>
    </button>
  );
};
