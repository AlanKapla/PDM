# Prompt UI-03: GenerateCostEstimateWithAIModal — Step 1 i Step 2

## Cel
Utwórz modal wielostepowy dla generowania kosztorysu z AI.
Ten prompt implementuje **Step 1** (formularz pytań) i **Step 2** (wybór szablonu).

---

## Plik do utworzenia

`src/components/GenerateCostEstimateWithAIModal.tsx`

Cały komponent — modal ma 5 kroków (Steps). Ten prompt implementuje kroki 1 i 2.
Kroki 3-5 będą uzupełnione w następnym promptcie (UI-04).

---

## Implementacja

```tsx
import { useState, useEffect } from 'react';
import {
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalFooter,
  ModalCloseButton,
  Button,
  VStack,
  HStack,
  FormControl,
  FormLabel,
  FormErrorMessage,
  Input,
  Textarea,
  Select,
  NumberInput,
  NumberInputField,
  Text,
  Box,
  Badge,
  Alert,
  AlertIcon,
  Spinner,
  Progress,
  Divider,
  Icon,
  Flex,
} from '@chakra-ui/react';
import { Bot, ChevronRight, ChevronLeft, AlertTriangle } from 'lucide-react';
import { Link as RouterLink } from 'react-router-dom';
import {
  costEstimateTemplateApi,
  type CostEstimateTemplateListItem,
  type CostEstimateTemplateStructureWeb,
} from '../api/costEstimateTemplateApi';
import { useToastNotification } from '../hooks/useToastNotification';
import { handleApiError } from '../utils/handleApiError';
import type {
  AICostEstimateRequestDto,
  AICostEstimatePreviewDto,
} from '../types/costEstimate.types.new';

// Stany wykończenia do wyboru
const FINISHING_STANDARDS = [
  { value: 'surowy_otwarty', label: 'Surowy otwarty' },
  { value: 'surowy_zamkniety', label: 'Surowy zamknięty' },
  { value: 'deweloperski', label: 'Deweloperski' },
  { value: 'pod_klucz', label: 'Pod klucz' },
] as const;

const AREA_UNITS = ['m²', 'mb', 'szt', 'kpl', 'm³'] as const;

const CURRENT_YEAR = new Date().getFullYear();
const YEAR_OPTIONS = Array.from({ length: 11 }, (_, i) => CURRENT_YEAR + i);

export type AIModalStep = 1 | 2 | 3 | 4 | 5;

export interface GenerateCostEstimateWithAIModalProps {
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  projectId: string;
  onCostEstimateCreated: (id: string) => void;
}

interface FormState {
  investmentType: string;
  finishingStandard: string;
  budget: string;
  area: string;
  areaUnit: string;
  location: string;
  completionYear: string;
  additionalRequirements: string;
}

const INITIAL_FORM: FormState = {
  investmentType: '',
  finishingStandard: '',
  budget: '',
  area: '',
  areaUnit: 'm²',
  location: '',
  completionYear: '',
  additionalRequirements: '',
};

interface TemplateWithStructure extends CostEstimateTemplateListItem {
  structure?: CostEstimateTemplateStructureWeb;
}

export default function GenerateCostEstimateWithAIModal({
  isOpen,
  onClose,
  tenantId,
  projectId,
  onCostEstimateCreated,
}: GenerateCostEstimateWithAIModalProps) {
  const { showError } = useToastNotification();

  const [step, setStep] = useState<AIModalStep>(1);
  const [form, setForm] = useState<FormState>(INITIAL_FORM);
  const [formErrors, setFormErrors] = useState<Partial<Record<keyof FormState, string>>>({});

  // Szablony
  const [templates, setTemplates] = useState<CostEstimateTemplateListItem[]>([]);
  const [selectedTemplateId, setSelectedTemplateId] = useState('');
  const [templateDetails, setTemplateDetails] = useState<TemplateWithStructure | null>(null);
  const [loadingTemplates, setLoadingTemplates] = useState(false);
  const [loadingTemplateDetails, setLoadingTemplateDetails] = useState(false);

  // Podgląd AI i zapis (wypełniane w UI-04)
  const [preview, setPreview] = useState<AICostEstimatePreviewDto | null>(null);
  const [finalName, setFinalName] = useState('');
  const [finalDescription, setFinalDescription] = useState('');

  // Reset przy otwarciu modalu
  useEffect(() => {
    if (isOpen) {
      setStep(1);
      setForm(INITIAL_FORM);
      setFormErrors({});
      setSelectedTemplateId('');
      setTemplateDetails(null);
      setPreview(null);
      setFinalName('');
      setFinalDescription('');
      loadTemplates();
    }
  }, [isOpen]);

  // Ładuj szczegóły szablonu po wyborze
  useEffect(() => {
    if (!selectedTemplateId) {
      setTemplateDetails(null);
      return;
    }
    loadTemplateDetails(selectedTemplateId);
  }, [selectedTemplateId]);

  const loadTemplates = async () => {
    setLoadingTemplates(true);
    try {
      const list = await costEstimateTemplateApi.getTemplates();
      setTemplates(list);
    } catch (error: unknown) {
      const { title, description } = handleApiError(error);
      showError(title, description);
    } finally {
      setLoadingTemplates(false);
    }
  };

  const loadTemplateDetails = async (templateId: string) => {
    setLoadingTemplateDetails(true);
    try {
      const details = await costEstimateTemplateApi.getTemplateDetails(templateId);
      const base = templates.find((t) => t.id === templateId);
      if (base) {
        setTemplateDetails({ ...base, structure: details.structure });
      }
    } catch {
      setTemplateDetails(null);
    } finally {
      setLoadingTemplateDetails(false);
    }
  };

  // Walidacja Step 1
  const validateStep1 = (): boolean => {
    const errors: Partial<Record<keyof FormState, string>> = {};
    if (!form.investmentType.trim()) {
      errors.investmentType = 'Opis inwestycji jest wymagany';
    } else if (form.investmentType.length > 1000) {
      errors.investmentType = 'Maksymalnie 1000 znaków';
    }
    if (form.budget && Number(form.budget) <= 0) {
      errors.budget = 'Budżet musi być większy od 0';
    }
    if (form.area && Number(form.area) <= 0) {
      errors.area = 'Powierzchnia musi być większa od 0';
    }
    setFormErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleStep1Next = () => {
    if (validateStep1()) setStep(2);
  };

  const handleStep2Next = () => {
    if (!selectedTemplateId) {
      showError('Wybierz szablon', 'Musisz wybrać szablon kosztorysu przed kontynuacją.');
      return;
    }
    setStep(3);
  };

  const buildRequest = (): AICostEstimateRequestDto => ({
    templateId: selectedTemplateId,
    investmentType: form.investmentType.trim(),
    finishingStandard: form.finishingStandard || undefined,
    budget: form.budget ? Number(form.budget) : undefined,
    area: form.area ? Number(form.area) : undefined,
    areaUnit: form.area ? form.areaUnit : undefined,
    location: form.location.trim() || undefined,
    completionYear: form.completionYear ? Number(form.completionYear) : undefined,
    additionalRequirements: form.additionalRequirements.trim() || undefined,
  });

  const handleClose = () => {
    onClose();
  };

  const stepTitles: Record<AIModalStep, string> = {
    1: 'Opisz inwestycję',
    2: 'Wybierz szablon',
    3: 'Generowanie AI...',
    4: 'Podgląd kosztorysu',
    5: 'Zatwierdź i zapisz',
  };

  const progressValue = (step / 5) * 100;

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      size={{ base: 'full', md: 'xl' }}
      scrollBehavior="inside"
      closeOnOverlayClick={step !== 3}
    >
      <ModalOverlay />
      <ModalContent>
        <ModalHeader>
          <HStack spacing={2}>
            <Icon as={Bot} color="purple.500" />
            <Text>Stwórz kosztorys z AI</Text>
          </HStack>
          <Text fontSize="sm" fontWeight="normal" color="gray.500" mt={1}>
            Krok {step} z 5 — {stepTitles[step]}
          </Text>
          <Progress value={progressValue} size="xs" colorScheme="purple" mt={2} borderRadius="full" />
        </ModalHeader>
        {step !== 3 && <ModalCloseButton />}

        <ModalBody>
          {step === 1 && (
            <Step1Form
              form={form}
              setForm={setForm}
              errors={formErrors}
            />
          )}
          {step === 2 && (
            <Step2Template
              templates={templates}
              selectedTemplateId={selectedTemplateId}
              onSelectTemplate={setSelectedTemplateId}
              templateDetails={templateDetails}
              loadingTemplates={loadingTemplates}
              loadingTemplateDetails={loadingTemplateDetails}
              tenantId={tenantId}
            />
          )}
          {/* Kroki 3-5 są implementowane przez komponent StepGenerating/StepPreview w UI-04 */}
          {(step === 3 || step === 4 || step === 5) && (
            <Box py={4}>
              <Text color="gray.500" textAlign="center">
                Ładowanie kolejnych kroków...
              </Text>
            </Box>
          )}
        </ModalBody>

        <ModalFooter>
          <HStack spacing={3} width="full" justify="space-between">
            <Button
              variant="ghost"
              leftIcon={<ChevronLeft size={16} />}
              onClick={() => step > 1 ? setStep((s) => (s - 1) as AIModalStep) : handleClose()}
              isDisabled={step === 3}
            >
              {step === 1 ? 'Anuluj' : 'Wstecz'}
            </Button>

            {step === 1 && (
              <Button
                colorScheme="purple"
                rightIcon={<ChevronRight size={16} />}
                onClick={handleStep1Next}
              >
                Dalej
              </Button>
            )}
            {step === 2 && (
              <Button
                colorScheme="purple"
                rightIcon={<ChevronRight size={16} />}
                onClick={handleStep2Next}
                isDisabled={!selectedTemplateId || loadingTemplateDetails}
              >
                Generuj z AI
              </Button>
            )}
          </HStack>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}

// ======== STEP 1: Formularz pytań ========

interface Step1FormProps {
  form: FormState;
  setForm: React.Dispatch<React.SetStateAction<FormState>>;
  errors: Partial<Record<keyof FormState, string>>;
}

function Step1Form({ form, setForm, errors }: Step1FormProps) {
  const update = (field: keyof FormState) => (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>
  ) => setForm((f) => ({ ...f, [field]: e.target.value }));

  return (
    <VStack spacing={4} align="stretch">
      <FormControl isRequired isInvalid={!!errors.investmentType}>
        <FormLabel>Co budujesz?</FormLabel>
        <Textarea
          value={form.investmentType}
          onChange={update('investmentType')}
          placeholder="np. Dom jednorodzinny 150m², remont mieszkania 60m², parking podziemny na 50 miejsc..."
          rows={3}
        />
        <FormErrorMessage>{errors.investmentType}</FormErrorMessage>
      </FormControl>

      <FormControl>
        <FormLabel>Stan wykończenia</FormLabel>
        <Select
          value={form.finishingStandard}
          onChange={update('finishingStandard')}
          placeholder="Wybierz stan wykończenia (opcjonalnie)"
        >
          {FINISHING_STANDARDS.map((s) => (
            <option key={s.value} value={s.value}>{s.label}</option>
          ))}
        </Select>
      </FormControl>

      <HStack spacing={3} align="flex-end">
        <FormControl isInvalid={!!errors.budget}>
          <FormLabel>Szacowany budżet (PLN brutto)</FormLabel>
          <Input
            type="number"
            value={form.budget}
            onChange={update('budget')}
            placeholder="np. 500000"
            min={0}
          />
          <FormErrorMessage>{errors.budget}</FormErrorMessage>
        </FormControl>
      </HStack>

      <HStack spacing={3} align="flex-end">
        <FormControl flex={3} isInvalid={!!errors.area}>
          <FormLabel>Powierzchnia / zakres</FormLabel>
          <Input
            type="number"
            value={form.area}
            onChange={update('area')}
            placeholder="np. 150"
            min={0}
          />
          <FormErrorMessage>{errors.area}</FormErrorMessage>
        </FormControl>
        <FormControl flex={2}>
          <FormLabel>Jednostka</FormLabel>
          <Select value={form.areaUnit} onChange={update('areaUnit')}>
            {AREA_UNITS.map((u) => (
              <option key={u} value={u}>{u}</option>
            ))}
          </Select>
        </FormControl>
      </HStack>

      <FormControl>
        <FormLabel>Lokalizacja</FormLabel>
        <Input
          value={form.location}
          onChange={update('location')}
          placeholder="np. Warszawa, Kraków, Śląsk"
        />
      </FormControl>

      <FormControl>
        <FormLabel>Rok ukończenia</FormLabel>
        <Select
          value={form.completionYear}
          onChange={update('completionYear')}
          placeholder="Wybierz rok (opcjonalnie)"
        >
          {YEAR_OPTIONS.map((y) => (
            <option key={y} value={y}>{y}</option>
          ))}
        </Select>
      </FormControl>

      <FormControl>
        <FormLabel>Dodatkowe wymagania</FormLabel>
        <Textarea
          value={form.additionalRequirements}
          onChange={update('additionalRequirements')}
          placeholder="np. ogrzewanie podłogowe, fotowoltaika 10kW, winda, basen..."
          rows={3}
          maxLength={2000}
        />
        <Text fontSize="xs" color="gray.400" textAlign="right" mt={1}>
          {form.additionalRequirements.length}/2000
        </Text>
      </FormControl>
    </VStack>
  );
}

// ======== STEP 2: Wybór szablonu ========

interface Step2TemplateProps {
  templates: CostEstimateTemplateListItem[];
  selectedTemplateId: string;
  onSelectTemplate: (id: string) => void;
  templateDetails: TemplateWithStructure | null;
  loadingTemplates: boolean;
  loadingTemplateDetails: boolean;
  tenantId: string;
}

function Step2Template({
  templates,
  selectedTemplateId,
  onSelectTemplate,
  templateDetails,
  loadingTemplates,
  loadingTemplateDetails,
  tenantId,
}: Step2TemplateProps) {
  if (loadingTemplates) {
    return (
      <Flex justify="center" py={8}>
        <Spinner color="purple.500" />
      </Flex>
    );
  }

  if (templates.length === 0) {
    return (
      <Alert status="warning" borderRadius="md">
        <AlertIcon as={AlertTriangle} />
        <VStack align="flex-start" spacing={1}>
          <Text fontWeight="semibold">Brak szablonów kosztorysów</Text>
          <Text fontSize="sm">
            Aby wygenerować kosztorys z AI, musisz najpierw{' '}
            <RouterLink
              to={`/tenants/${tenantId}/cost-estimate-templates`}
              style={{ color: 'var(--chakra-colors-blue-500)', textDecoration: 'underline' }}
            >
              utworzyć szablon kosztorysu
            </RouterLink>
            .
          </Text>
        </VStack>
      </Alert>
    );
  }

  return (
    <VStack spacing={4} align="stretch">
      <FormControl isRequired>
        <FormLabel>Szablon kosztorysu</FormLabel>
        <Select
          value={selectedTemplateId}
          onChange={(e) => onSelectTemplate(e.target.value)}
          placeholder="Wybierz szablon..."
        >
          {templates.map((t) => (
            <option key={t.id} value={t.id}>
              {t.name}
            </option>
          ))}
        </Select>
      </FormControl>

      {loadingTemplateDetails && (
        <Flex justify="center" py={4}>
          <Spinner size="sm" color="purple.500" />
        </Flex>
      )}

      {templateDetails?.structure && !loadingTemplateDetails && (
        <Box
          borderWidth="1px"
          borderColor="purple.200"
          borderRadius="md"
          p={4}
          bg="purple.50"
        >
          <Text fontWeight="semibold" mb={2}>{templateDetails.name}</Text>
          {templateDetails.description && (
            <Text fontSize="sm" color="gray.600" mb={2}>{templateDetails.description}</Text>
          )}
          <Divider mb={2} />
          <HStack spacing={4} flexWrap="wrap">
            {templateDetails.structure.groupHeaderFields?.length > 0 && (
              <Badge colorScheme="purple" variant="subtle">
                {templateDetails.structure.groupHeaderFields.length} pól grup
              </Badge>
            )}
            {templateDetails.structure.systemFields?.length > 0 && (
              <Badge colorScheme="blue" variant="subtle">
                {templateDetails.structure.systemFields.length} pól systemowych
              </Badge>
            )}
            {templateDetails.structure.calculatedFields?.length > 0 && (
              <Badge colorScheme="green" variant="subtle">
                {templateDetails.structure.calculatedFields.length} pól obliczeniowych
              </Badge>
            )}
            {templateDetails.structure.units?.length > 0 && (
              <Badge colorScheme="gray" variant="subtle">
                {templateDetails.structure.units.length} jednostek
              </Badge>
            )}
          </HStack>
        </Box>
      )}

      <Alert status="info" borderRadius="md" fontSize="sm">
        <AlertIcon />
        AI wygeneruje strukturę kosztorysu na podstawie szablonu. Możesz edytować wynik przed zapisem.
      </Alert>
    </VStack>
  );
}
```

---

## Eksporty
Komponent eksportuje:
- `default GenerateCostEstimateWithAIModal`
- `type AIModalStep`
- `type GenerateCostEstimateWithAIModalProps`

---

## Uwagi implementacyjne
1. Sprawdź dokładne właściwości `CostEstimateTemplateStructureWeb` — może mieć `groupHeaderFields` lub `groupFieldDefinitions`. Dostosuj nazwy do aktualnego interfejsu w `costEstimateTemplateApi.ts`.
2. `RouterLink` do szablonów — sprawdź aktualną ścieżkę do szablonów w routerze (`src/routes/`). Dostosuj path jeśli jest inny.
3. Nie dodawaj logiki kroków 3-5 tutaj — zostaną one uzupełnione w UI-04.

## Weryfikacja
```
npx tsc --noEmit 2>&1 | Select-String "GenerateCostEstimateWithAIModal|error TS" | Select-Object -First 20
```
