import { useState, useEffect, useContext } from 'react';
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
  Accordion,
  AccordionItem,
  AccordionButton,
  AccordionPanel,
  AccordionIcon,
  Tag,
} from '@chakra-ui/react';
import { Bot, ChevronRight, ChevronLeft, AlertTriangle, Check, FileText, Folder } from 'lucide-react';
import { Link as RouterLink } from 'react-router-dom';
import {
  costEstimateTemplateApi,
  type CostEstimateTemplateListItem,
  type CostEstimateTemplateStructureWeb,
} from '../api/costEstimateTemplateApi';
import { useToastNotification } from '../hooks/useToastNotification';
import { handleApiError } from '../utils/handleApiError';
import { AuthContext } from '../context/AuthContext';
import { useGenerateCostEstimateWithAI } from '../hooks/useGenerateCostEstimateWithAI';
import type {
  AICostEstimateRequestDto,
  AICostEstimatePreviewDto,
} from '../types/costEstimate.types.new';

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

  const [templates, setTemplates] = useState<CostEstimateTemplateListItem[]>([]);
  const [selectedTemplateId, setSelectedTemplateId] = useState<string>('');
  const [templateDetails, setTemplateDetails] = useState<TemplateWithStructure | null>(null);
  const [loadingTemplates, setLoadingTemplates] = useState<boolean>(false);
  const [loadingTemplateDetails, setLoadingTemplateDetails] = useState<boolean>(false);

  // Placeholder state for UI-04 steps
  const [preview, setPreview] = useState<AICostEstimatePreviewDto | null>(null);
  const [finalName, setFinalName] = useState<string>('');
  const [finalDescription, setFinalDescription] = useState<string>('');

  const { user: _user } = useContext(AuthContext);
  const { generatePreview, createFromPreview } = useGenerateCostEstimateWithAI(tenantId, projectId);

  // Uruchom generowanie gdy wchodzimy na step 3
  useEffect(() => {
    if (step === 3 && !preview && !generatePreview.isPending) {
      const request = buildRequest();
      generatePreview.mutate(request, {
        onSuccess: (result) => {
          setPreview(result);
          setFinalName(result.suggestedName);
          setFinalDescription(result.suggestedDescription ?? '');
          setStep(4);
        },
        onError: (error: Error) => {
          const { title, description } = handleApiError(error);
          showError(title, description);
          setStep(2);
        },
      });
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [step]);

  const handleSave = (): void => {
    if (!preview) return;
    if (!finalName.trim()) {
      showError('Nazwa wymagana', 'Podaj nazwę kosztorysu przed zapisem.');
      return;
    }
    createFromPreview.mutate(
      { name: finalName.trim(), description: finalDescription.trim() || undefined, preview },
      {
        onSuccess: (id) => {
          onCostEstimateCreated(id);
          handleClose();
        },
        onError: (error: Error) => {
          const { title, description } = handleApiError(error);
          showError(title, description);
        },
      }
    );
  };

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
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isOpen]);

  useEffect(() => {
    if (!selectedTemplateId) {
      setTemplateDetails(null);
      return;
    }
    loadTemplateDetails(selectedTemplateId);
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedTemplateId]);

  const loadTemplates = async (): Promise<void> => {
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

  const loadTemplateDetails = async (templateId: string): Promise<void> => {
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

  const handleStep1Next = (): void => {
    if (validateStep1()) setStep(2);
  };

  const handleStep2Next = (): void => {
    if (!selectedTemplateId) {
      showError('Wybierz szablon', 'Musisz wybrać szablon kosztorysu przed kontynuacją.');
      return;
    }
    setStep(3);
  };

  // Will be used in UI-04
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

  const handleClose = (): void => {
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
            <Icon as={Bot} color="purple.500" aria-hidden="true" />
            <Text>Stwórz kosztorys z AI</Text>
          </HStack>
          <Text fontSize="sm" fontWeight="normal" color="gray.600" mt={1}>
            Krok {step} z 5 — {stepTitles[step]}
          </Text>
          <Progress value={progressValue} size="xs" colorScheme="purple" mt={2} borderRadius="full" />
        </ModalHeader>
        {step !== 3 && <ModalCloseButton aria-label="Zamknij modal" />}

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
          {step === 3 && (
            <Step3Generating />
          )}
          {step === 4 && preview && (
            <Step4Preview preview={preview} />
          )}
          {step === 5 && preview && (
            <Step5Confirm
              preview={preview}
              finalName={finalName}
              finalDescription={finalDescription}
              onNameChange={setFinalName}
              onDescriptionChange={setFinalDescription}
            />
          )}
        </ModalBody>

        <ModalFooter>
          <HStack spacing={3} width="full" justify="space-between">
            <Button
              variant="ghost"
              leftIcon={<ChevronLeft size={16} aria-hidden="true" />}
              onClick={() => step > 1 ? setStep((s) => (s - 1) as AIModalStep) : handleClose()}
              isDisabled={step === 3}
            >
              {step === 1 ? 'Anuluj' : 'Wstecz'}
            </Button>

            {step === 1 && (
              <Button
                colorScheme="purple"
                rightIcon={<ChevronRight size={16} aria-hidden="true" />}
                onClick={handleStep1Next}
              >
                Dalej
              </Button>
            )}
            {step === 2 && (
              <Button
                colorScheme="purple"
                rightIcon={<ChevronRight size={16} aria-hidden="true" />}
                onClick={handleStep2Next}
                isDisabled={!selectedTemplateId || loadingTemplateDetails}
              >
                Generuj z AI
              </Button>
            )}
            {step === 4 && (
              <Button
                colorScheme="purple"
                rightIcon={<ChevronRight size={16} />}
                onClick={() => setStep(5)}
              >
                Zatwierdź podgląd
              </Button>
            )}
            {step === 5 && (
              <Button
                colorScheme="green"
                leftIcon={<Check size={16} />}
                onClick={handleSave}
                isLoading={createFromPreview.isPending}
                loadingText="Zapisywanie..."
              >
                Zapisz kosztorys
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
        <FormErrorMessage role="alert">{errors.investmentType}</FormErrorMessage>
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

      <FormControl isInvalid={!!errors.budget}>
        <FormLabel>Szacowany budżet (PLN brutto)</FormLabel>
        <Input
          type="number"
          value={form.budget}
          onChange={update('budget')}
          placeholder="np. 500000"
          min={0}
        />
        <FormErrorMessage role="alert">{errors.budget}</FormErrorMessage>
      </FormControl>

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
          <FormErrorMessage role="alert">{errors.area}</FormErrorMessage>
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
  tenantId: _tenantId,
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
      <Alert status="warning" borderRadius="md" role="alert">
        <AlertIcon as={AlertTriangle} aria-hidden="true" />
        <VStack align="flex-start" spacing={1}>
          <Text fontWeight="semibold">Brak szablonów kosztorysów</Text>
          <Text fontSize="sm">
            Aby wygenerować kosztorys z AI, musisz najpierw{' '}
            <RouterLink
              to="/cost-estimate-templates"
            >
              <Text as="span" color="blue.500" textDecoration="underline">
                utworzyć szablon kosztorysu
              </Text>
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
          onChange={(e: React.ChangeEvent<HTMLSelectElement>) => onSelectTemplate(e.target.value)}
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
            {(templateDetails.structure.groupHeaderFields?.length ?? 0) > 0 && (
              <Badge colorScheme="purple" variant="subtle">
                {templateDetails.structure.groupHeaderFields.length} pól grup
              </Badge>
            )}
            {(templateDetails.structure.systemFields?.length ?? 0) > 0 && (
              <Badge colorScheme="blue" variant="subtle">
                {templateDetails.structure.systemFields.length} pól systemowych
              </Badge>
            )}
            {(templateDetails.structure.calculatedFields?.length ?? 0) > 0 && (
              <Badge colorScheme="green" variant="subtle">
                {templateDetails.structure.calculatedFields.length} pól obliczeniowych
              </Badge>
            )}
            {(templateDetails.structure.units?.length ?? 0) > 0 && (
              <Badge colorScheme="gray" variant="subtle">
                {templateDetails.structure.units.length} jednostek
              </Badge>
            )}
          </HStack>
        </Box>
      )}

      <Alert status="info" borderRadius="md" fontSize="sm">
        <AlertIcon aria-hidden="true" />
        AI wygeneruje strukturę kosztorysu na podstawie szablonu. Możesz edytować wynik przed zapisem.
      </Alert>
    </VStack>
  );
}

// ======== STEP 3: Generowanie AI ========

function Step3Generating() {
  return (
    <VStack spacing={6} py={8} align="center">
      <Spinner size="xl" color="purple.500" thickness="4px" />
      <VStack spacing={1}>
        <Text fontWeight="semibold" fontSize="lg">AI generuje kosztorys...</Text>
        <Text color="gray.500" fontSize="sm" textAlign="center">
          Analizuję opis inwestycji i strukturę szablonu.
          Może to potrwać do 30 sekund.
        </Text>
      </VStack>
    </VStack>
  );
}

// ======== STEP 4: Podgląd drzewa ========

interface Step4PreviewProps {
  preview: AICostEstimatePreviewDto;
}

function Step4Preview({ preview }: Step4PreviewProps) {
  return (
    <VStack spacing={4} align="stretch">
      {preview.warnings.length > 0 && (
        <Alert status="warning" borderRadius="md" fontSize="sm">
          <AlertIcon />
          <VStack align="flex-start" spacing={0}>
            <Text fontWeight="semibold">Ostrzeżenia AI</Text>
            {preview.warnings.map((w, i) => (
              <Text key={i} fontSize="xs">{w}</Text>
            ))}
          </VStack>
        </Alert>
      )}

      <Box>
        <Text fontWeight="semibold" fontSize="sm" mb={2} color="gray.600">
          Sugerowana nazwa:{' '}
          <Text as="span" color="purple.600">{preview.suggestedName}</Text>
        </Text>
        {preview.suggestedDescription && (
          <Text fontSize="sm" color="gray.500" mb={2}>{preview.suggestedDescription}</Text>
        )}
      </Box>

      <Text fontWeight="semibold" mb={1}>
        Struktura kosztorysu ({preview.groups.length} grup)
      </Text>

      <Accordion allowMultiple defaultIndex={preview.groups.map((_, i) => i)}>
        {preview.groups
          .filter((g) => !g.parentTempId)
          .sort((a, b) => a.order - b.order)
          .map((group) => (
            <GroupPreviewItem
              key={group.tempId}
              group={group}
              allGroups={preview.groups}
              indent={0}
            />
          ))}
      </Accordion>
    </VStack>
  );
}

interface GroupPreviewItemProps {
  group: import('../types/costEstimate.types.new').AIGroupPreviewDto;
  allGroups: import('../types/costEstimate.types.new').AIGroupPreviewDto[];
  indent: number;
}

function GroupPreviewItem({ group, allGroups, indent }: GroupPreviewItemProps) {
  const subGroups = allGroups
    .filter((g) => g.parentTempId === group.tempId)
    .sort((a, b) => a.order - b.order);

  return (
    <AccordionItem borderColor="purple.100">
      <AccordionButton pl={indent * 4 + 2}>
        <HStack flex={1} textAlign="left" spacing={2}>
          <Folder size={14} color="var(--chakra-colors-purple-500)" aria-hidden="true" />
          <Text fontWeight="medium" fontSize="sm">{group.name}</Text>
          <Tag size="sm" colorScheme="blue" variant="subtle">
            {group.items.length} poz.
          </Tag>
        </HStack>
        <AccordionIcon />
      </AccordionButton>
      <AccordionPanel pb={2} pl={indent * 4 + 4}>
        {group.items.length > 0 && (
          <VStack align="stretch" spacing={1} mb={2}>
            {group.items.sort((a, b) => a.order - b.order).map((item) => (
              <HStack
                key={item.tempId}
                spacing={2}
                py={1}
                px={2}
                borderRadius="md"
                bg="gray.50"
                fontSize="sm"
              >
                <FileText size={12} color="var(--chakra-colors-gray-400)" aria-hidden="true" />
                <Text flex={1} noOfLines={1}>{item.name}</Text>
                {item.fieldValues.length > 0 && (
                  <Tag size="sm" colorScheme="gray" variant="subtle">
                    {item.fieldValues.length} pól
                  </Tag>
                )}
              </HStack>
            ))}
          </VStack>
        )}
        {subGroups.length > 0 && (
          <Accordion allowMultiple>
            {subGroups.map((sg) => (
              <GroupPreviewItem
                key={sg.tempId}
                group={sg}
                allGroups={allGroups}
                indent={indent + 1}
              />
            ))}
          </Accordion>
        )}
        {group.items.length === 0 && subGroups.length === 0 && (
          <Text fontSize="xs" color="gray.400" fontStyle="italic">Pusta grupa</Text>
        )}
      </AccordionPanel>
    </AccordionItem>
  );
}

// ======== STEP 5: Potwierdzenie + edycja nazwy ========

interface Step5ConfirmProps {
  preview: AICostEstimatePreviewDto;
  finalName: string;
  finalDescription: string;
  onNameChange: (v: string) => void;
  onDescriptionChange: (v: string) => void;
}

function Step5Confirm({
  preview,
  finalName,
  finalDescription,
  onNameChange,
  onDescriptionChange,
}: Step5ConfirmProps) {
  const totalItems = preview.groups.reduce((sum, g) => sum + g.items.length, 0);

  return (
    <VStack spacing={4} align="stretch">
      <Alert status="success" borderRadius="md">
        <AlertIcon />
        <VStack align="flex-start" spacing={0}>
          <Text fontWeight="semibold">Kosztorys gotowy do zapisu</Text>
          <Text fontSize="sm">
            {preview.groups.length} grup, {totalItems} pozycji. Sprawdź nazwę i kliknij "Zapisz kosztorys".
          </Text>
        </VStack>
      </Alert>

      <FormControl isRequired>
        <FormLabel>Nazwa kosztorysu</FormLabel>
        <Input
          value={finalName}
          onChange={(e) => onNameChange(e.target.value)}
          placeholder="Nazwa kosztorysu"
          maxLength={200}
        />
      </FormControl>

      <FormControl>
        <FormLabel>Opis (opcjonalny)</FormLabel>
        <Textarea
          value={finalDescription}
          onChange={(e) => onDescriptionChange(e.target.value)}
          placeholder="Krótki opis kosztorysu..."
          rows={3}
          maxLength={2000}
        />
      </FormControl>

      {preview.warnings.length > 0 && (
        <Alert status="warning" borderRadius="md" fontSize="sm">
          <AlertIcon />
          <VStack align="flex-start" spacing={0}>
            <Text fontWeight="semibold">Pola pominięte przez AI</Text>
            {preview.warnings.slice(0, 5).map((w, i) => (
              <Text key={i} fontSize="xs">{w}</Text>
            ))}
            {preview.warnings.length > 5 && (
              <Text fontSize="xs" color="gray.500">...i {preview.warnings.length - 5} więcej</Text>
            )}
          </VStack>
        </Alert>
      )}
    </VStack>
  );
}
