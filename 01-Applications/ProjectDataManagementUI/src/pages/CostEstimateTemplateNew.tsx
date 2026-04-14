import { useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Box,
  Heading,
  VStack,
  HStack,
  Text,
  Button,
  Input,
  Textarea,
  FormControl,
  FormLabel,
  Divider,
  Alert,
  AlertIcon,
} from "@chakra-ui/react";
import { FileText, Save, ArrowLeft } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import { costEstimateTemplateApi } from "../api/costEstimateTemplateApi";
import { useToastNotification } from "../hooks/useToastNotification";

export default function CostEstimateTemplateNew() {
  const navigate = useNavigate();
  const { showSuccess, showError } = useToastNotification();
  
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [templateName, setTemplateName] = useState("");
  const [templateDescription, setTemplateDescription] = useState("");

  const handleSubmit = async () => {
    if (!templateName.trim()) {
      showError("Błąd walidacji", "Nazwa szablonu jest wymagana");
      return;
    }

    setIsSubmitting(true);

    try {
      const result = await costEstimateTemplateApi.createTemplate({
        name: templateName,
        description: templateDescription || undefined,
      });

      showSuccess("Sukces", "Szablon został utworzony. Teraz możesz dodać pola, waluty i jednostki.");

      // Przekieruj do edycji nowo utworzonego szablonu (result to ID szablonu)
      navigate(`/cost-estimate-templates/${result}/edit`);
    } catch (error) {
      showError("Błąd", "Nie udało się utworzyć szablonu");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <MainLayout>
      <Box maxW="800px" mx="auto" p={6}>
        {/* Header */}
        <HStack justify="space-between" mb={6}>
          <HStack spacing={3}>
            <FileText size={32} />
            <Heading size="lg">Nowy szablon kosztorysu</Heading>
          </HStack>
        </HStack>

        {/* Main Content */}
        <VStack spacing={6} align="stretch">
          <Alert status="info">
            <AlertIcon />
            Po utworzeniu szablonu będziesz mógł skonfigurować szczegółową strukturę pól w edycji.
          </Alert>

          {/* Podstawowe informacje */}
          <Box bg="white" p={6} borderRadius="lg" shadow="sm" borderWidth="1px">
            <Heading size="md" mb={4}>Podstawowe informacje</Heading>
            <VStack spacing={4} align="stretch">
              <FormControl isRequired>
                <FormLabel>Nazwa szablonu</FormLabel>
                <Input
                  value={templateName}
                  onChange={(e) => setTemplateName(e.target.value)}
                  placeholder="np. Szablon dla projektów budowlanych"
                  autoFocus
                />
              </FormControl>

              <FormControl>
                <FormLabel>Opis</FormLabel>
                <Textarea
                  value={templateDescription}
                  onChange={(e) => setTemplateDescription(e.target.value)}
                  placeholder="Opcjonalny opis szablonu"
                  rows={3}
                />
              </FormControl>
            </VStack>
          </Box>

          <Divider />

          {/* Footer Actions */}
          <HStack justify="space-between" pt={4}>
            <Button
              leftIcon={<ArrowLeft size={18} />}
              variant="ghost"
              onClick={() => navigate("/cost-estimate-templates")}
            >
              Anuluj
            </Button>
            <Button
              leftIcon={<Save size={18} />}
              colorScheme="primary"
              onClick={handleSubmit}
              isLoading={isSubmitting}
              loadingText="Tworzenie..."
            >
              Utwórz szablon
            </Button>
          </HStack>
        </VStack>
      </Box>
    </MainLayout>
  );
}
