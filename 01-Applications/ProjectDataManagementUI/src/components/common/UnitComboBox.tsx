import React, { useState, useEffect, useRef, useCallback } from 'react';
import ReactDOM from 'react-dom';
import { Box, Input, Text } from '@chakra-ui/react';

// ---------------------------------------------------------------------------
// Typy
// ---------------------------------------------------------------------------

export interface UnitOption {
  id: string;
  code: string;
  name: string;
  symbol: string;
}

export interface UnitComboBoxProps {
  units: UnitOption[];
  value: string | undefined;
  onChange: (value: string | undefined) => void;
  disabled?: boolean;
}

// ---------------------------------------------------------------------------
// Komponent
// ---------------------------------------------------------------------------

/**
 * Input z podpowiedziami jednostek z szablonu.
 * Dropdown renderowany przez portal — nie jest obcinany przez overflow:hidden.
 */
export const UnitComboBox: React.FC<UnitComboBoxProps> = ({
  units,
  value,
  onChange,
  disabled,
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const [inputValue, setInputValue] = useState(value || '');
  const inputRef = useRef<HTMLInputElement>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const [dropdownStyle, setDropdownStyle] = useState<React.CSSProperties>({});

  // Synchronizuj inputValue z zewnętrzną wartością
  useEffect(() => {
    setInputValue(value || '');
  }, [value]);

  // Filtruj jednostki po wpisanym tekście
  const filtered = units.filter((u) => {
    const search = inputValue.toLowerCase();
    return (
      u.code.toLowerCase().includes(search) ||
      u.name.toLowerCase().includes(search) ||
      (u.symbol && u.symbol.toLowerCase().includes(search))
    );
  });

  // Oblicz pozycję dropdowna na ekranie (portal renderuje poza overflow:hidden)
  const updateDropdownPosition = useCallback(() => {
    if (inputRef.current) {
      const rect = inputRef.current.getBoundingClientRect();
      setDropdownStyle({
        position: 'fixed',
        top: rect.bottom + 2,
        left: rect.left,
        width: rect.width,
        zIndex: 9999,
      });
    }
  }, []);

  const openDropdown = useCallback(() => {
    updateDropdownPosition();
    setIsOpen(true);
  }, [updateDropdownPosition]);

  // Zamknij dropdown przy kliknięciu poza inputem i dropdownem
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      const target = e.target as Node;
      if (
        inputRef.current &&
        !inputRef.current.contains(target) &&
        dropdownRef.current &&
        !dropdownRef.current.contains(target)
      ) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  // Zamknij dropdown przy scrollu (pozycja się zmieni)
  useEffect(() => {
    if (!isOpen) return;
    const handleScroll = () => setIsOpen(false);
    window.addEventListener('scroll', handleScroll, true);
    return () => window.removeEventListener('scroll', handleScroll, true);
  }, [isOpen]);

  const dropdown =
    isOpen && filtered.length > 0
      ? ReactDOM.createPortal(
          <Box
            ref={dropdownRef}
            style={dropdownStyle}
            bg="white"
            border="1px solid"
            borderColor="gray.200"
            borderRadius="md"
            boxShadow="lg"
            maxH="180px"
            overflowY="auto"
          >
            {filtered.map((unit) => (
              <Box
                key={unit.id}
                px={3}
                py={1.5}
                fontSize="sm"
                cursor="pointer"
                _hover={{ bg: 'blue.50' }}
                bg={value === unit.code ? 'blue.100' : undefined}
                onClick={() => {
                  onChange(unit.code);
                  setInputValue(unit.code);
                  setIsOpen(false);
                }}
              >
                <Text fontWeight="medium">{unit.code}</Text>
                {unit.name !== unit.code && (
                  <Text fontSize="xs" color="gray.500">
                    {unit.name}
                    {unit.symbol ? ` (${unit.symbol})` : ''}
                  </Text>
                )}
              </Box>
            ))}
          </Box>,
          document.body
        )
      : null;

  return (
    <>
      <Input
        ref={inputRef}
        value={inputValue}
        onChange={(e) => {
          const v = e.target.value;
          setInputValue(v);
          onChange(v || undefined);
          openDropdown();
        }}
        onClick={openDropdown}
        onFocus={openDropdown}
        onKeyDown={(e) => {
          if (e.key === 'Escape') setIsOpen(false);
        }}
        isDisabled={disabled}
        size="sm"
        variant="outline"
        placeholder="Jednostka..."
        bg="white"
        borderColor="gray.300"
        _hover={{ borderColor: 'blue.400' }}
        _focus={{
          borderColor: 'blue.500',
          boxShadow: '0 0 0 1px var(--chakra-colors-blue-500)',
        }}
      />
      {dropdown}
    </>
  );
};
