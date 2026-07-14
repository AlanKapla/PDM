import React, { useEffect, useState } from 'react';
import {
  Alert,
  AlertIcon,
  Badge,
  Button,
  FormControl,
  FormLabel,
  HStack,
  Input,
  NumberInput,
  NumberInputField,
  SimpleGrid,
  Text,
  Textarea,
  VStack,
} from '@chakra-ui/react';
import { ContractorPicker } from '../ContractorPicker';
import { CostCategoryPicker } from '../CostCategoryPicker';
import ContractorQuickAddModal from '../ContractorQuickAddModal';
import { CostCategoryQuickAddModal } from '../CostCategoryQuickAddModal';
import type {
  CostDocumentType,
  ParsedCostDto,
  SuggestedContractor,
  SuggestedCostCategory,
} from '../../types/ai.types';

export interface AICostReviewItemFormProps {
  tenantId: string;
  projectId: string;
  costDocumentType: CostDocumentType;
  parsedData: ParsedCostDto;
  onChange: (data: ParsedCostDto) => void;
  canQuickAdd: boolean;
  isDisabled?: boolean;
}

function getSuggestedContractor(data: ParsedCostDto): SuggestedContractor | undefined {
  if (data.suggestedContractor) {
    return data.suggestedContractor;
  }

  if (!data.contractorFound && data.contractorName) {
    return {
      name: data.contractorName,
      nip: data.contractorNip,
      address: data.contractorAddress,
    };
  }

  return undefined;
}

function getSuggestedCategory(data: ParsedCostDto): SuggestedCostCategory | undefined {
  if (data.suggestedCategory) {
    return data.suggestedCategory;
  }

  if (!data.categoryFound && data.categoryName) {
    return {
      name: data.categoryName,
    };
  }

  return undefined;
}

export function AICostReviewItemForm({
  tenantId,
  projectId,
  parsedData,
  onChange,
  canQuickAdd,
  isDisabled = false,
}: AICostReviewItemFormProps): React.ReactElement {
  const [localData, setLocalData] = useState<ParsedCostDto>(parsedData);
  const [isAiContractorCreateOpen, setIsAiContractorCreateOpen] = useState(false);
  const [isAiCategoryCreateOpen, setIsAiCategoryCreateOpen] = useState(false);

  useEffect(() => {
    setLocalData(parsedData);
  }, [parsedData]);

  const updateField = <K extends keyof ParsedCostDto>(
    field: K,
    value: ParsedCostDto[K]
  ): void => {
    const next: ParsedCostDto = { ...localData, [field]: value };
    setLocalData(next);
    onChange(next);
  };

  const updateFields = (patch: Partial<ParsedCostDto>): void => {
    const next: ParsedCostDto = { ...localData, ...patch };
    setLocalData(next);
    onChange(next);
  };

  const suggestedContractor = getSuggestedContractor(localData);
  const suggestedCategory = getSuggestedCategory(localData);

  return (
    <>
      <VStack spacing={4} align="stretch">
        <FormControl isRequired>
          <FormLabel>Nazwa kosztu</FormLabel>
          <Input
            value={localData.name}
            onChange={(event) => updateField('name', event.target.value)}
            isDisabled={isDisabled}
          />
        </FormControl>

        <FormControl>
          <FormLabel>Opis</FormLabel>
          <Textarea
            value={localData.description ?? ''}
            onChange={(event) => updateField('description', event.target.value || undefined)}
            rows={3}
            isDisabled={isDisabled}
          />
        </FormControl>

        <FormControl>
          <FormLabel>Numer dokumentu</FormLabel>
          <Input
            value={localData.number ?? ''}
            onChange={(event) => updateField('number', event.target.value || undefined)}
            isDisabled={isDisabled}
          />
        </FormControl>

        <FormControl>
          <FormLabel>Data</FormLabel>
          <Input
            type="date"
            value={localData.date ? localData.date.substring(0, 10) : ''}
            onChange={(event) => updateField('date', event.target.value || undefined)}
            isDisabled={isDisabled}
          />
        </FormControl>

        <SimpleGrid columns={2} spacing={3}>
          <FormControl>
            <FormLabel>Kwota netto (zł)</FormLabel>
            <NumberInput
              value={localData.net ?? ''}
              onChange={(_valueString: string, valueNumber: number) =>
                updateField('net', Number.isNaN(valueNumber) ? undefined : valueNumber)
              }
              min={0}
              precision={2}
              isDisabled={isDisabled}
            >
              <NumberInputField />
            </NumberInput>
          </FormControl>

          <FormControl>
            <FormLabel>Kwota brutto (zł)</FormLabel>
            <NumberInput
              value={localData.gross ?? ''}
              onChange={(_valueString: string, valueNumber: number) =>
                updateField('gross', Number.isNaN(valueNumber) ? undefined : valueNumber)
              }
              min={0}
              precision={2}
              isDisabled={isDisabled}
            >
              <NumberInputField />
            </NumberInput>
          </FormControl>
        </SimpleGrid>

        <FormControl>
          <HStack mb={1} spacing={2} align="center">
            <FormLabel mb={0}>Kontrahent</FormLabel>
            {localData.contractorFound && localData.contractorId && (
              <Badge colorScheme="purple" fontSize="2xs" px={1.5} py={0.5}>
                ⚡ AI znalazł
              </Badge>
            )}
          </HStack>
          <ContractorPicker
            tenantId={tenantId}
            value={localData.contractorId ?? null}
            onChange={(id: string | null) => {
              updateFields({
                contractorId: id ?? undefined,
                contractorFound: id !== null,
              });
            }}
            canQuickAdd={canQuickAdd}
            isDisabled={isDisabled}
          />
          {!localData.contractorFound && suggestedContractor && !localData.contractorId && (
            <Alert status="warning" mt={2} fontSize="sm" role="alert">
              <AlertIcon />
              <VStack align="flex-start" flex={1} spacing={1}>
                <Text fontSize="sm">
                  AI sugeruje: <strong>{suggestedContractor.name}</strong>
                  {suggestedContractor.nip && <> · NIP: {suggestedContractor.nip}</>}
                </Text>
                {canQuickAdd && (
                  <Button
                    size="xs"
                    colorScheme="purple"
                    onClick={() => setIsAiContractorCreateOpen(true)}
                    isDisabled={isDisabled}
                  >
                    Utwórz kontrahenta
                  </Button>
                )}
              </VStack>
            </Alert>
          )}
        </FormControl>

        <FormControl>
          <HStack mb={1} spacing={2} align="center">
            <FormLabel mb={0}>Kategoria</FormLabel>
            {localData.categoryFound && localData.categoryId && (
              <Badge colorScheme="purple" fontSize="2xs" px={1.5} py={0.5}>
                ⚡ AI znalazł
              </Badge>
            )}
          </HStack>
          <CostCategoryPicker
            tenantId={tenantId}
            projectId={projectId}
            value={localData.categoryId ?? null}
            onChange={(id: string | null) => {
              updateFields({
                categoryId: id ?? undefined,
                categoryFound: id !== null,
              });
            }}
            canQuickAdd={canQuickAdd}
            isDisabled={isDisabled}
          />
          {!localData.categoryFound && suggestedCategory && !localData.categoryId && (
            <Alert status="warning" mt={2} fontSize="sm" role="alert">
              <AlertIcon />
              <VStack align="flex-start" flex={1} spacing={1}>
                <Text fontSize="sm">
                  AI sugeruje: <strong>{suggestedCategory.name}</strong>
                  {suggestedCategory.code && <> · Kod: {suggestedCategory.code}</>}
                </Text>
                {canQuickAdd && (
                  <Button
                    size="xs"
                    colorScheme="purple"
                    onClick={() => setIsAiCategoryCreateOpen(true)}
                    isDisabled={isDisabled}
                  >
                    Utwórz kategorię
                  </Button>
                )}
              </VStack>
            </Alert>
          )}
        </FormControl>
      </VStack>

      {isAiContractorCreateOpen && suggestedContractor && (
        <ContractorQuickAddModal
          isOpen
          tenantId={tenantId}
          onClose={() => setIsAiContractorCreateOpen(false)}
          initialValues={{
            name: suggestedContractor.name,
            taxId: suggestedContractor.nip,
            street: suggestedContractor.address,
          }}
          onCreated={(id: string) => {
            updateFields({
              contractorId: id,
              contractorFound: true,
            });
            setIsAiContractorCreateOpen(false);
          }}
        />
      )}

      {isAiCategoryCreateOpen && suggestedCategory && (
        <CostCategoryQuickAddModal
          isOpen
          tenantId={tenantId}
          projectId={projectId}
          onClose={() => setIsAiCategoryCreateOpen(false)}
          initialValues={{
            name: suggestedCategory.name,
            code: suggestedCategory.code,
          }}
          onCreated={(id: string) => {
            updateFields({
              categoryId: id,
              categoryFound: true,
            });
            setIsAiCategoryCreateOpen(false);
          }}
        />
      )}
    </>
  );
}
