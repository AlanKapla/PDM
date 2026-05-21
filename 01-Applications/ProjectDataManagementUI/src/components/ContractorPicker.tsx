import React, { useState, useRef, useMemo, useCallback } from "react";
import {
  Box,
  Input,
  InputGroup,
  InputLeftElement,
  InputRightElement,
  HStack,
  IconButton,
  Tooltip,
  VStack,
  Text,
  useColorModeValue,
} from "@chakra-ui/react";
import { Search, X, Plus } from "lucide-react";
import { useContractors } from "../hooks/queries/useContractors";
import ContractorQuickAddModal from "./ContractorQuickAddModal";
import type { ContractorWeb } from "../types/contractor.types";

interface ContractorPickerProps {
  tenantId: string;
  value: string | null;
  onChange: (id: string | null) => void;
  canQuickAdd?: boolean;
  isDisabled?: boolean;
  isInvalid?: boolean;
  placeholder?: string;
}

export function ContractorPicker({
  tenantId,
  value,
  onChange,
  canQuickAdd = false,
  isDisabled = false,
  isInvalid = false,
}: ContractorPickerProps): React.ReactElement {
  const { data: contractors = [], isLoading } = useContractors(tenantId);
  const [isQuickAddOpen, setIsQuickAddOpen] = useState(false);
  const [search, setSearch] = useState("");
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  const dropdownBg = useColorModeValue("white", "gray.800");
  const dropdownBorder = useColorModeValue("gray.200", "gray.600");
  const itemHoverBg = useColorModeValue("primary.50", "primary.900");

  const selected = useMemo<ContractorWeb | null>(
    () => contractors.find((c) => c.id === value) ?? null,
    [contractors, value]
  );

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return contractors;
    return contractors.filter(
      (c) =>
        c.name.toLowerCase().includes(q) ||
        (c.taxId?.toLowerCase().includes(q) ?? false) ||
        (c.city?.toLowerCase().includes(q) ?? false) ||
        (c.email?.toLowerCase().includes(q) ?? false) ||
        (c.phoneNumber?.toLowerCase().includes(q) ?? false)
    );
  }, [contractors, search]);

  const handleSelect = useCallback(
    (contractor: ContractorWeb) => {
      onChange(contractor.id);
      setSearch("");
      setIsOpen(false);
    },
    [onChange]
  );

  const handleClear = useCallback(() => {
    onChange(null);
    setSearch("");
  }, [onChange]);

  const handleInputFocus = () => {
    setIsOpen(true);
    setSearch("");
  };

  const handleInputBlur = (e: React.FocusEvent) => {
    // Zamknij dropdown tylko jeśli focus opuszcza cały kontener
    if (!containerRef.current?.contains(e.relatedTarget as Node)) {
      setIsOpen(false);
      setSearch("");
    }
  };

  const displayValue = isOpen ? search : (selected?.name ?? "");

  return (
    <>
      <HStack align="flex-start">
        <Box position="relative" flex={1} ref={containerRef} onBlur={handleInputBlur}>
          <InputGroup>
            <InputLeftElement pointerEvents="none">
              <Search size={16} />
            </InputLeftElement>
            <Input
              value={displayValue}
              onChange={(e) => setSearch(e.target.value)}
              onFocus={handleInputFocus}
              placeholder={isLoading ? "Ładowanie..." : "Szukaj po nazwie, NIP, mieście..."}
              isDisabled={isDisabled || isLoading}
              isInvalid={isInvalid}
              pr={value ? "2.5rem" : undefined}
              autoComplete="off"
            />
            {value && !isDisabled && (
              <InputRightElement>
                <IconButton
                  aria-label="Wyczyść"
                  icon={<X size={14} />}
                  size="xs"
                  variant="ghost"
                  onMouseDown={(e) => { e.preventDefault(); handleClear(); }}
                />
              </InputRightElement>
            )}
          </InputGroup>

          {isOpen && (
            <Box
              position="absolute"
              top="100%"
              left={0}
              right={0}
              zIndex={1000}
              bg={dropdownBg}
              border="1px solid"
              borderColor={dropdownBorder}
              borderRadius="md"
              boxShadow="md"
              maxH="220px"
              overflowY="auto"
              mt={1}
            >
              {filtered.length === 0 ? (
                <Text px={3} py={2} fontSize="sm" color="gray.500">
                  Brak wyników
                </Text>
              ) : (
                <VStack spacing={0} align="stretch">
                  <Box
                    px={3}
                    py={2}
                    cursor="pointer"
                    fontSize="sm"
                    color="gray.500"
                    _hover={{ bg: itemHoverBg }}
                    onMouseDown={(e) => { e.preventDefault(); onChange(null); setIsOpen(false); setSearch(""); }}
                  >
                    — Brak kontrahenta —
                  </Box>
                  {filtered.map((c) => (
                    <Box
                      key={c.id}
                      px={3}
                      py={2}
                      cursor="pointer"
                      bg={c.id === value ? itemHoverBg : undefined}
                      fontWeight={c.id === value ? "semibold" : "normal"}
                      _hover={{ bg: itemHoverBg }}
                      onMouseDown={(e) => { e.preventDefault(); handleSelect(c); }}
                    >
                      <Text fontSize="sm">{c.name}</Text>
                      {(c.taxId || c.city) && (
                        <Text fontSize="xs" color="gray.500">
                          {[c.taxId ? `NIP: ${c.taxId}` : null, c.city].filter(Boolean).join(" · ")}
                        </Text>
                      )}
                    </Box>
                  ))}
                </VStack>
              )}
            </Box>
          )}
        </Box>

        {canQuickAdd && (
          <Tooltip label="Dodaj nowego kontrahenta">
            <IconButton
              aria-label="Dodaj nowego kontrahenta"
              icon={<Plus size={16} />}
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
        <ContractorQuickAddModal
          isOpen={isQuickAddOpen}
          onClose={() => setIsQuickAddOpen(false)}
          tenantId={tenantId}
          onCreated={(id) => {
            onChange(id);
            setIsQuickAddOpen(false);
          }}
        />
      )}
    </>
  );
}

export default ContractorPicker;

