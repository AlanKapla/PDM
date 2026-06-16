import React, { useState, useEffect, useCallback } from 'react';
import {
  Box,
  Input,
  List,
  ListItem,
  Text,
  Spinner,
} from '@chakra-ui/react';

interface UnitComboboxProps {
  value: string;
  units: string[];
  onChange: (value: string) => void;
  onBlur?: () => void;
  onAddNewUnit?: (code: string) => void;
  isAddingUnit?: boolean;
  isDisabled?: boolean;
  placeholder?: string;
  w?: string;
  /** W TreeView — bez białego tła przy hover/focus */
  blendWithRow?: boolean;
  textAlign?: 'left' | 'right';
}

export const UnitCombobox: React.FC<UnitComboboxProps> = ({
  value,
  units,
  onChange,
  onBlur,
  onAddNewUnit,
  isAddingUnit = false,
  isDisabled,
  placeholder = '',
  w = 'full',
  blendWithRow = false,
  textAlign = 'left',
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const [inputValue, setInputValue] = useState(value);

  useEffect(() => {
    setInputValue(value);
  }, [value]);

  const filtered = inputValue
    ? units.filter((u) => u.toLowerCase().startsWith(inputValue.toLowerCase()))
    : units;

  const isExactMatch = inputValue
    ? units.some((u) => u.toLowerCase() === inputValue.toLowerCase())
    : true;

  const showAddOption = !!(
    inputValue &&
    inputValue.trim().length > 0 &&
    !isExactMatch &&
    onAddNewUnit
  );

  const handleSelect = useCallback(
    (unit: string) => {
      setInputValue(unit);
      onChange(unit);
      setIsOpen(false);
      onBlur?.();
    },
    [onChange, onBlur]
  );

  const handleInputChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      const v = e.target.value;
      setInputValue(v);
      onChange(v);
      setIsOpen(true);
    },
    [onChange]
  );

  const handleAddNew = useCallback(() => {
    if (inputValue.trim() && onAddNewUnit) {
      onAddNewUnit(inputValue.trim());
    }
  }, [inputValue, onAddNewUnit]);

  const handleBlur = useCallback(() => {
    setTimeout(() => {
      setIsOpen(false);
      onBlur?.();
    }, 150);
  }, [onBlur]);

  const hasListItems = filtered.length > 0 || showAddOption;

  return (
    <Box position="relative" w={w}>
      <Input
        value={inputValue}
        onChange={handleInputChange}
        onFocus={() => !isDisabled && setIsOpen(true)}
        onBlur={handleBlur}
        isDisabled={isDisabled}
        placeholder={placeholder}
        size="sm"
        fontSize="xs"
        bg="transparent"
        border="none"
        borderRadius="6px"
        px={1}
        _focus={
          blendWithRow
            ? {
                bg: 'transparent',
                border: '1px solid',
                borderColor: 'primary.300',
                boxShadow: '0 0 0 2px rgba(47,108,236,0.12)',
              }
            : {
                bg: 'white',
                border: '1px solid',
                borderColor: 'primary.300',
                boxShadow: '0 0 0 2px rgba(47,108,236,0.12)',
              }
        }
        _hover={
          !isDisabled
            ? blendWithRow
              ? { bg: 'transparent' }
              : { bg: 'white' }
            : undefined
        }
        cursor={isDisabled ? 'not-allowed' : 'text'}
        autoComplete="off"
        textAlign={textAlign}
        aria-label="Jednostka miary"
        aria-autocomplete="list"
        aria-expanded={isOpen}
      />
      {isOpen && hasListItems && !isAddingUnit && (
        <Box
          position="absolute"
          top="100%"
          left={0}
          zIndex={20}
          bg="white"
          border="1px solid"
          borderColor="neutral.200"
          borderRadius="8px"
          boxShadow="0 4px 16px rgba(20,33,47,0.12)"
          maxH="200px"
          overflowY="auto"
          minW="120px"
          mt="2px"
        >
          <List>
            {filtered.map((unit) => (
              <ListItem
                key={unit}
                px={3}
                py={1.5}
                fontSize="sm"
                cursor="pointer"
                _hover={{ bg: 'primary.50', color: 'primary.700' }}
                bg={unit === value ? 'primary.25' : undefined}
                fontWeight={unit === value ? 'semibold' : 'normal'}
                onMouseDown={() => handleSelect(unit)}
                role="option"
                aria-selected={unit === value}
              >
                {unit}
              </ListItem>
            ))}
            {showAddOption && (
              <ListItem
                px={3}
                py={1.5}
                fontSize="sm"
                cursor="pointer"
                borderTop="1px solid"
                borderColor="neutral.100"
                color="primary.600"
                fontWeight="medium"
                _hover={{ bg: 'primary.50', color: 'primary.700' }}
                onMouseDown={handleAddNew}
                role="option"
                aria-label={`Dodaj jednostkę "${inputValue.trim()}"`}
              >
                <Text as="span">+ Dodaj „{inputValue.trim()}”</Text>
              </ListItem>
            )}
          </List>
        </Box>
      )}
      {isOpen && isAddingUnit && (
        <Box
          position="absolute"
          top="100%"
          left={0}
          zIndex={20}
          bg="white"
          border="1px solid"
          borderColor="neutral.200"
          borderRadius="8px"
          boxShadow="0 4px 16px rgba(20,33,47,0.12)"
          minW="120px"
          mt="2px"
          px={3}
          py={2}
        >
          <Spinner size="sm" />
        </Box>
      )}
    </Box>
  );
};
