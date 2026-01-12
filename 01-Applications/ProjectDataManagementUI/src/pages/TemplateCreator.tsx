import { useState } from "react";
import {
  Box,
  Heading,
  VStack,
  HStack,
  Checkbox,
  Button,
  Input,
  Text,
  useToast,
  Divider,
  Tag,
} from "@chakra-ui/react";

import MainLayout from "../layout/MainLayout";

type SectionKey = "materials" | "labor" | "equipment";

interface SectionFieldDef {
  key: string;
  label: string;
}

const SECTION_DEFS: Record<SectionKey, { label: string; fields: SectionFieldDef[] }> = {
  materials: {
    label: "Materiały",
    fields: [
      { key: "name", label: "Nazwa pozycji" },
      { key: "unit", label: "Jednostka" },
      { key: "quantity", label: "Ilość" },
      { key: "unitNetPrice", label: "Cena netto" },
      { key: "vat", label: "VAT (%)" },
      { key: "netValue", label: "Wartość netto (auto)" },
      { key: "grossValue", label: "Wartość brutto (auto)" },
    ],
  },
  labor: {
    label: "Robocizna",
    fields: [
      { key: "workerCategory", label: "Kategoria pracownika" },
      { key: "hourRate", label: "Stawka/h" },
      { key: "hours", label: "Godziny" },
      { key: "laborCost", label: "Koszt robocizny (auto)" },
    ],
  },
  equipment: {
    label: "Sprzęt",
    fields: [
      { key: "equipmentType", label: "Typ sprzętu" },
      { key: "equipmentRate", label: "Stawka sprzętu/h" },
      { key: "equipmentHours", label: "Godziny sprzętu" },
      { key: "equipmentCost", label: "Koszt sprzętu (auto)" },
    ],
  },
};
export default function TemplateCreator() {
  const toast = useToast();

  const [templateName, setTemplateName] = useState("");
  const [enabledSections, setEnabledSections] = useState<Record<SectionKey, boolean>>({
    materials: true,
    labor: false,
    equipment: false,
  });

  const [selectedFields, setSelectedFields] = useState<Record<SectionKey, string[]>>({
    materials: ["name", "unit", "quantity", "unitNetPrice", "vat", "netValue", "grossValue"],
    labor: ["workerCategory", "hourRate", "hours", "laborCost"],
    equipment: ["equipmentType", "equipmentRate", "equipmentHours", "equipmentCost"],
  });

  const toggleSection = (section: SectionKey) => {
    setEnabledSections(prev => ({
      ...prev,
      [section]: !prev[section],
    }));
  };

  const toggleField = (section: SectionKey, fieldKey: string) => {
    setSelectedFields(prev => {
      const current = prev[section] ?? [];
      const exists = current.includes(fieldKey);
      const next = exists
        ? current.filter(k => k !== fieldKey)
        : [...current, fieldKey];

      return { ...prev, [section]: next };
    });
  };

  const buildTemplateJson = () => {
    const sectionsPayload = (Object.keys(SECTION_DEFS) as SectionKey[])
      .filter(secKey => enabledSections[secKey])
      .map(secKey => ({
        key: secKey,
        label: SECTION_DEFS[secKey].label,
        fields: selectedFields[secKey] ?? [],
      }));

    return {
      templateName: templateName || "Nowy kosztorys",
      sections: sectionsPayload,
    };
  };

  const handleValidate = async () => {
    const payload = buildTemplateJson();

    try {
      const response = await fetch("/api/cost-templates/validate", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
      });

      const json = await response.json();

      if (!response.ok) {
        const errors: string[] = json.errors ?? ["Nieznany błąd walidacji."];
        toast({
          title: "Szablon niepoprawny",
          description: errors.join("\n"),
          status: "error",
          duration: 8000,
          isClosable: true,
        });
        return;
      }

      toast({
        title: "Szablon poprawny",
        description: "Możesz przejść do uzupełniania kosztorysu.",
        status: "success",
        duration: 5000,
        isClosable: true,
      });

      // jeśli backend zwraca templateId — tutaj możesz zapisać go w stanie / navigate:
      // navigate(`/cost-editor/${json.templateId}`);
    } catch (err) {
      console.error(err);
      toast({
        title: "Błąd połączenia",
        description: "Nie udało się skontaktować z serwerem.",
        status: "error",
        duration: 5000,
        isClosable: true,
      });
    }
  };

  const templatePreviewJson = JSON.stringify(buildTemplateJson(), null, 2);

  return (
    <MainLayout>
      <Box p={{ base: 3, sm: 4, md: 6 }}>
        <Heading size="md" mb={4}>
          Kreator szablonu kosztorysu
        </Heading>

        <VStack align="stretch" spacing={4}>
          {/* Nazwa szablonu */}
          <Box>
            <Text mb={1} fontWeight="semibold">
              Nazwa szablonu
            </Text>
            <Input
              placeholder="Np. Kosztorys budowlany"
              value={templateName}
              onChange={e => setTemplateName(e.target.value)}
            />
          </Box>

          <Divider />

          {/* Sekcje + pola */}
          <HStack align="flex-start" spacing={10}>
            {(Object.keys(SECTION_DEFS) as SectionKey[]).map(secKey => {
              const section = SECTION_DEFS[secKey];
              const enabled = enabledSections[secKey];

              return (
                <VStack key={secKey} align="flex-start" spacing={2}>
                  <Checkbox
                    isChecked={enabled}
                    onChange={() => toggleSection(secKey)}
                  >
                    <Text fontWeight="bold">{section.label}</Text>
                  </Checkbox>

                  {enabled && (
                    <VStack align="flex-start" spacing={1} pl={4}>
                      {section.fields.map(field => (
                        <Checkbox
                          key={field.key}
                          isChecked={selectedFields[secKey]?.includes(field.key)}
                          onChange={() => toggleField(secKey, field.key)}
                        >
                          {field.label}
                        </Checkbox>
                      ))}
                    </VStack>
                  )}
                </VStack>
              );
            })}
          </HStack>

          <Divider />

          {/* Akcje */}
          <HStack spacing={4}>
            <Button colorScheme="blue" onClick={handleValidate}>
              Wyślij szablon do walidacji
            </Button>
            <Tag size="lg" colorScheme="gray">
              JSON wysyłany do backendu
            </Tag>
          </HStack>

          {/* Podgląd JSON-a (debug / dev) */}
          <Box mt={4}>
            <Box
              as="pre"
              fontSize="sm"
              p={3}
              bg="gray.900"
              color="green.200"
              borderRadius="md"
              overflowX="auto"
            >
              {templatePreviewJson}
            </Box>
          </Box>
        </VStack>
      </Box>
    </MainLayout>
  );
}
