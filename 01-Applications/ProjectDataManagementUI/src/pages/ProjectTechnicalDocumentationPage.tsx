import { useNavigate, useParams } from 'react-router-dom';
import {
  Box,
  Button,
  Heading,
  HStack,
  Icon,
  Table,
  Tbody,
  Td,
  Text,
  Th,
  Thead,
  Tr,
  useColorModeValue,
  useDisclosure,
  VStack,
} from '@chakra-ui/react';
import { Plus, ScanLine } from 'lucide-react';
import MainLayout from '../layout/MainLayout';
import { BackToProjectButton, LoadingSpinner, EmptyState } from '../components/common';
import { AddTechnicalDocumentationModal } from '../components/technicalDocumentation/AddTechnicalDocumentationModal';
import { TechnicalDocumentationStatusBadge } from '../components/technicalDocumentation/TechnicalDocumentationStatusBadge';
import { useAuth } from '../context/AuthContext';
import { useProjectPermissions } from '../hooks/useProjectPermissions';
import {
  useProjectDetails,
  useTechnicalDocumentationList,
} from '../hooks/queries';
import { formatDate } from '../utils/formatters';
import type { TechnicalDocumentationListItemWeb } from '../types/technicalDocumentation.types';

export default function ProjectTechnicalDocumentationPage(): React.ReactElement {
  const { projectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const permissions = useProjectPermissions(projectId);
  const { isOpen, onOpen, onClose } = useDisclosure();

  const tenantId = user?.activeTenantId;
  const canView = permissions.canViewTechnicalDocumentation;

  const { data: project } = useProjectDetails(tenantId ?? undefined, projectId);
  const {
    data: documentationList = [],
    isLoading,
  } = useTechnicalDocumentationList(
    tenantId ?? undefined,
    projectId,
    canView && !permissions.loading
  );

  const hoverBg = useColorModeValue('gray.50', 'gray.700');
  const borderColor = useColorModeValue('gray.200', 'gray.700');

  const handleRowClick = (item: TechnicalDocumentationListItemWeb): void => {
    navigate(`/projects/${projectId}/technical-documentation/${item.id}`);
  };

  return (
    <MainLayout>
      <VStack spacing={6} align="stretch" p={{ base: 3, sm: 4, md: 8 }}>
        <HStack justify="space-between" flexWrap="wrap" gap={3}>
          <HStack spacing={3}>
            <BackToProjectButton projectId={projectId!} />
            <Icon as={ScanLine} boxSize={8} color="teal.600" aria-hidden="true" />
            <VStack align="flex-start" spacing={0}>
              <Heading size="lg">Dokumentacja techniczna</Heading>
              {project && (
                <Text fontSize="sm" color="neutral.600">
                  {project.name}
                </Text>
              )}
            </VStack>
          </HStack>

          {canView && permissions.canWriteTechnicalDocumentation && (
            <Button
              leftIcon={<Plus size={18} aria-hidden="true" />}
              colorScheme="primary"
              onClick={onOpen}
            >
              Dodaj dokumentację
            </Button>
          )}
        </HStack>

        {!canView && !permissions.loading ? (
          <Box textAlign="center">
            <EmptyState
              icon={ScanLine}
              title="Brak dostępu"
              description="Nie masz uprawnień do przeglądania dokumentacji technicznej w tym projekcie"
            />
          </Box>
        ) : isLoading ? (
          <LoadingSpinner message="Ładowanie dokumentacji..." />
        ) : documentationList.length === 0 ? (
          <EmptyState
            icon={ScanLine}
            title="Brak dokumentacji"
            description="Nie dodano jeszcze żadnej dokumentacji technicznej w tym projekcie"
            action={
              permissions.canWriteTechnicalDocumentation ? (
                <Button colorScheme="primary" onClick={onOpen}>
                  Dodaj dokumentację
                </Button>
              ) : undefined
            }
          />
        ) : (
          <Box overflowX="auto" borderWidth="1px" borderColor={borderColor} borderRadius="md">
            <Table size="sm">
              <Thead>
                <Tr>
                  <Th>Nazwa</Th>
                  <Th>Opis</Th>
                  <Th>Status</Th>
                  <Th isNumeric>Pliki</Th>
                  <Th>Utworzono</Th>
                </Tr>
              </Thead>
              <Tbody>
                {documentationList.map((item) => (
                  <Tr
                    key={item.id}
                    cursor="pointer"
                    _hover={{ bg: hoverBg }}
                    onClick={() => handleRowClick(item)}
                  >
                    <Td>
                      <Text fontWeight="medium" noOfLines={1}>
                        {item.name}
                      </Text>
                    </Td>
                    <Td maxW="200px">
                      <Text fontSize="sm" color="neutral.600" noOfLines={1}>
                        {item.description || '—'}
                      </Text>
                    </Td>
                    <Td>
                      <TechnicalDocumentationStatusBadge status={item.status} />
                    </Td>
                    <Td isNumeric>
                      <Text fontSize="sm">{item.fileCount}</Text>
                    </Td>
                    <Td>
                      <Text fontSize="sm" color="neutral.700">
                        {formatDate(item.createdAt)}
                      </Text>
                    </Td>
                  </Tr>
                ))}
              </Tbody>
            </Table>
          </Box>
        )}
      </VStack>

      {tenantId && projectId && (
        <AddTechnicalDocumentationModal
          isOpen={isOpen}
          onClose={onClose}
          tenantId={tenantId}
          projectId={projectId}
        />
      )}
    </MainLayout>
  );
}
