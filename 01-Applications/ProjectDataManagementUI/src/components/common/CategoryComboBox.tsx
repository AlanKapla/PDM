import React, { useState, useEffect, useRef, useCallback, useId } from 'react';
import ReactDOM from 'react-dom';
import { Box, Input, Text } from '@chakra-ui/react';

// ---------------------------------------------------------------------------
// Typy
// ---------------------------------------------------------------------------

export interface CategoryOption {
  id: string;
  name: string;
  symbol: string | null;
}

export interface CategoryComboBoxProps {
  categories: CategoryOption[];
  /** Wartość zapisywana = category.name */
  value: string | undefined;
  onChange: (value: string | undefined) => void;
  disabled?: boolean;
}

// ---------------------------------------------------------------------------
// Komponent
// ---------------------------------------------------------------------------

/**
 * Input z podpowiedziami kategorii z szablonu.
 * Dropdown renderowany przez portal — nie jest obcinany przez overflow:hidden.
 * Wzorzec identyczny jak UnitComboBox.
 * Wartość zapisywana = category.name, etykieta = category.symbol ?? category.name.
 */
export const CategoryComboBox: React.FC<CategoryComboBoxProps> = ({
  categories,
  value,
  onChange,
  disabled,
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const [inputValue, setInputValue] = useState(value || '');
  const [activeIndex, setActiveIndex] = useState<number | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const [dropdownStyle, setDropdownStyle] = useState<React.CSSProperties>({});

  // Synchronizuj inputValue z zewnętrzną wartością
  useEffect(() => {
    setInputValue(value || '');
  }, [value]);

  // Filtruj kategorie po wpisanym tekście (name i symbol)
  const filtered = categories.filter((c) => {
    const search = inputValue.toLowerCase();
    return (
      c.name.toLowerCase().includes(search) ||
      (c.symbol && c.symbol.toLowerCase().includes(search))
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
    setActiveIndex(0);
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

  const reactId = useId();
  const listboxId = `category-combobox-listbox-${reactId}`;

  const getOptionBg = (index: number): string | undefined => {
  if (index === activeIndex) return 'primary.100';
  if (filtered[index]?.name === value) return 'primary.50';
    return undefined;
  };

  const clampIndex = (index: number): number =>
    Math.max(0, Math.min(index, filtered.length - 1));

  const selectCategory = (cat: CategoryOption) => {
    onChange(cat.name);
    setInputValue(cat.name);
    setIsOpen(false);
    setActiveIndex(null);
  };

  const dropdown =
    isOpen && filtered.length > 0
      ? ReactDOM.createPortal(
          <Box
            ref={dropdownRef}
            id={listboxId}
            role="listbox"
            style={dropdownStyle}
            bg="white"
            border="1px solid"
            borderColor="neutral.200"
            borderRadius="md"
            boxShadow="lg"
            maxH="180px"
            overflowY="auto"
          >
            {filtered.map((cat, index) => (
              <Box
                key={cat.id}
                id={`category-option-${reactId}-${cat.id}`}
                role="option"
                aria-selected={value === cat.name}
                px={3}
                py={1.5}
                fontSize="sm"
                cursor="pointer"
                _hover={{ bg: 'primary.50' }}
                bg={getOptionBg(index)}
                onMouseEnter={() => setActiveIndex(index)}
                onClick={() => selectCategory(cat)}
              >
                <Text fontWeight="medium">{cat.symbol ?? cat.name}</Text>
                {cat.symbol && cat.symbol !== cat.name && (
                  <Text fontSize="xs" color="neutral.500">
                    {cat.name}
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
        role="combobox"
        aria-expanded={isOpen}
        aria-autocomplete="list"
        aria-controls={isOpen ? listboxId : undefined}
        aria-activedescendant={
          isOpen && activeIndex !== null && filtered[activeIndex]
            ? `category-option-${reactId}-${filtered[activeIndex].id}`
            : undefined
        }
        value={inputValue}
        onChange={(e) => {
          const v = e.target.value;
          setInputValue(v);
          onChange(v || undefined);
          openDropdown();
          setActiveIndex(0);
        }}
        onClick={openDropdown}
        onFocus={openDropdown}
        onKeyDown={(e) => {
          if (e.key === 'Escape') {
            setIsOpen(false);
            setActiveIndex(null);
            return;
          }
          if (e.key === 'ArrowDown') {
            e.preventDefault();
            if (!isOpen) {
              openDropdown();
            } else if (filtered.length > 0) {
              setActiveIndex((prev) => clampIndex((prev === null ? -1 : prev) + 1));
            }
            return;
          }
          if (e.key === 'ArrowUp') {
            e.preventDefault();
            if (!isOpen) {
              openDropdown();
            } else if (filtered.length > 0) {
              setActiveIndex((prev) => clampIndex((prev === null ? 0 : prev) - 1));
            }
            return;
          }
          if (e.key === 'Enter' && isOpen && activeIndex !== null && filtered[activeIndex]) {
            e.preventDefault();
            selectCategory(filtered[activeIndex]);
          }
        }}
        isDisabled={disabled}
        size="sm"
        variant="outline"
        placeholder="Kategoria..."
        bg="white"
        borderColor="neutral.300"
        _hover={{ borderColor: 'primary.400' }}
        _focus={{
          borderColor: 'primary.500',
          boxShadow: '0 0 0 1px var(--chakra-colors-primary-500)',
        }}
      />
      {dropdown}
    </>
  );
};
