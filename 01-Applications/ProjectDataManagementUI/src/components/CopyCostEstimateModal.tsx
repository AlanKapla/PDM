import { useState, useEffect, useContext } from "react";
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
  Text,
  Checkbox,
  CheckboxGroup,
  Box,
  useColorModeValue,
  Spinner,
  Alert,
  AlertIcon,
} from "@chakra-ui/react";
import { AuthContext } from "../context/AuthContext";
import { projectApi } from "../api/projectApi";
import { costEstimateApi } from "../api/costEstimateApi";
import { useToastNotification } from "../hooks/useToastNotification";
import { handleApiError } from "../utils/handleApiError";

interface CopyCostEstimateModalProps {
  isOpen: boolean;
  onClose: () => void;
  costEstimateId: string;
  costEstimateName: string;
  currentProjectId: string;
  onSuccess?: () => void;
}

export default function CopyCostEstimateModal({
  isOpen,
  onClose,
  costEstimateId,
  costEstimateName,
  currentProjectId,
  onSuccess,
}: CopyCostEstimateModalProps) {
  const { user } = useContext(AuthContext);
  const {showSuccess, showError, showApiSuccess, showApiError } = useToastNotification();

  const [projects, setProjects] = useState<Record<string, string>>({});
  const [selectedProjectIds, setSelectedProjectIds] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [copying, setCopying] = useState(false);

  const borderColor = useColorModeValue("gray.200", "gray.600");
  const hoverBg = useColorModeValue("gray.50", "gray.700");

  useEffect(() => {
    if (isOpen && user?.activeTenantId) {
      fetchProjects();
    }
  }, [isOpen, user?.activeTenantId]);

  const fetchProjects = async () => {
    if (!user?.activeTenantId) return;

    setLoading(true);
    try {
      const projectsDict = await projectApi.getProjectsDictionary(user.activeTenantId);
      
      // Usuń bieżący projekt z listy
      const filteredProjects = Object.fromEntries(
        Object.entries(projectsDict).filter(([id]) => id !== currentProjectId)
      );
      
      setProjects(filteredProjects);
    } catch (error) {
      showApiError(error);
    } finally {
      setLoading(false);
    }
  };

  const handleCopy = async () => {
    if (!user?.activeTenantId || selectedProjectIds.length === 0) return;

    setCopying(true);
    try {
      const newCostEstimateIds = await costEstimateApi.copyCostEstimate(
        user.activeTenantId,
        currentProjectId,
        costEstimateId,
        selectedProjectIds
      );

      showApiSuccess('estimateCopied');

      onSuccess?.();
      handleClose();
    } catch (error) {
      showApiError(error);
    } finally {
      setCopying(false);
    }
  };

  const handleClose = () => {
    setSelectedProjectIds([]);
    onClose();
  };

  const projectEntries = Object.entries(projects);

  return (
    <Modal isOpen={isOpen} onClose={handleClose} size={{ base: "full", md: "lg" }} isCentered scrollBehavior="inside">
      <ModalOverlay />
      <ModalContent>
        <ModalHeader>Kopiuj kosztorys do innych projektów</ModalHeader>
        <ModalCloseButton />
        <ModalBody>
          <VStack spacing={4} align="stretch">
            <Box>
              <Text fontWeight="bold" mb={2}>
                Kosztorys:
              </Text>
              <Text fontSize="sm" color="gray.600">
                {costEstimateName}
              </Text>
            </Box>

            {loading ? (
              <Box textAlign="center" py={8}>
                <Spinner size="lg" color="primary.500" />
                <Text mt={3} fontSize="sm" color="gray.600">
                  Ładowanie projektów...
                </Text>
              </Box>
            ) : projectEntries.length === 0 ? (
              <Alert status="info" borderRadius="md">
                <AlertIcon />
                Brak innych projektów do kopiowania
              </Alert>
            ) : (
              <Box>
                <Text fontWeight="bold" mb={3}>
                  Wybierz projekty docelowe:
                </Text>
                <VStack
                  spacing={2}
                  align="stretch"
                  maxH="300px"
                  overflowY="auto"
                  p={3}
                  borderWidth="1px"
                  borderColor={borderColor}
                  borderRadius="md"
                >
                  {projectEntries.map(([id, name]) => (
                    <Box
                      key={id}
                      p={2}
                      borderRadius="md"
                      _hover={{ bg: hoverBg }}
                      transition="background 0.2s"
                    >
                      <Checkbox
                        value={id}
                        isChecked={selectedProjectIds.includes(id)}
                        onChange={(e) => {
                          if (e.target.checked) {
                            setSelectedProjectIds([...selectedProjectIds, id]);
                          } else {
                            setSelectedProjectIds(selectedProjectIds.filter((pid) => pid !== id));
                          }
                        }}
                      >
                        {name}
                      </Checkbox>
                    </Box>
                  ))}
                </VStack>
                <Text fontSize="xs" color="gray.500" mt={2}>
                  Wybrano: {selectedProjectIds.length} projekt(ów)
                </Text>
              </Box>
            )}
          </VStack>
        </ModalBody>

        <ModalFooter>
          <Button variant="ghost" mr={3} onClick={handleClose} isDisabled={copying}>
            Anuluj
          </Button>
          <Button
            colorScheme="primary"
            onClick={handleCopy}
            isLoading={copying}
            isDisabled={selectedProjectIds.length === 0 || loading}
          >
            Kopiuj
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}
