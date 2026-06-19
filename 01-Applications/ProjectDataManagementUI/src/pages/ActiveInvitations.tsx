import { useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Box,
  Heading,
  Text,
  Spinner,
  VStack,
  useColorModeValue,
  Button,
  HStack,
  Stack,
  Badge,
} from "@chakra-ui/react";
import { Mail, FolderKanban } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import { formatDateShort } from "../utils/formatters";
import { acceptTenantInvitation, changeActiveTenant } from "../services/tenantService";
import {
  useActiveInvitations,
  tenantKeys,
} from "../hooks/queries";
import { useQueryClient } from "@tanstack/react-query";
import { useToastNotification } from "../hooks/useToastNotification";
import { handleApiError } from "../utils/handleApiError";
import { InvitationStatus } from "../types/auth.types";
import type { TenantInvitationWeb } from "../types/auth.types";
import { PROJECT_MODULE_LABELS, ProjectModule } from "../types/projectModulePermissions";

export default function ActiveInvitations(): React.ReactElement {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { data: tenantInvitations = [], isLoading: loadingTenant } = useActiveInvitations();
  const [acceptingId, setAcceptingId] = useState<string | null>(null);
  const { showError, showApiSuccess } = useToastNotification();

  const pendingTenant = tenantInvitations.filter(
    (inv) => inv.status === InvitationStatus.Pending && !inv.projectId
  );
  const pendingProject = tenantInvitations.filter(
    (inv) => inv.status === InvitationStatus.Pending && inv.projectId
  );
  const loading = loadingTenant;
  const hasAny = pendingTenant.length > 0 || pendingProject.length > 0;

  const cardBg = useColorModeValue("white", "gray.800");
  const pageBg = useColorModeValue("gray.50", "gray.900");
  const borderColor = useColorModeValue("gray.200", "gray.600");

  const handleAcceptTenant = async (invitationId: string, token: string, tenantId: string): Promise<void> => {
    setAcceptingId(invitationId);
    try {
      const success = await acceptTenantInvitation(token);
      if (success) {
        await changeActiveTenant(tenantId);
        queryClient.invalidateQueries({ queryKey: tenantKeys.invitations() });
        queryClient.invalidateQueries({ queryKey: tenantKeys.my() });
        showApiSuccess("inviteAccepted");
      } else {
        showError("Nie udało się zaakceptować zaproszenia", "Zaproszenie może być nieaktualne lub wygasłe");
      }
    } catch (error) {
      const { title, description } = handleApiError(error);
      showError(title, description);
    } finally {
      setAcceptingId(null);
    }
  };

  const handleAcceptProject = async (inv: TenantInvitationWeb): Promise<void> => {
    setAcceptingId(inv.invitationId);
    try {
      const success = await acceptTenantInvitation(inv.token);
      if (!success) {
        showError("Nie udało się zaakceptować zaproszenia", "Zaproszenie może być nieaktualne lub wygasłe");
        return;
      }
      await changeActiveTenant(inv.tenantId);
      queryClient.invalidateQueries({ queryKey: tenantKeys.invitations() });
      queryClient.invalidateQueries({ queryKey: tenantKeys.my() });
      showApiSuccess("inviteAccepted");
      if (inv.projectId) {
        navigate(`/projects/${inv.projectId}`);
      }
    } catch (error) {
      const { title, description } = handleApiError(error);
      showError(title, description);
    } finally {
      setAcceptingId(null);
    }
  };

  const formatModules = (modules: number[]): string =>
    modules.map((m) => PROJECT_MODULE_LABELS[m as ProjectModule]).join(", ");

  if (loading) {
    return (
      <MainLayout>
        <VStack spacing={4} align="center" justify="center" minH="50vh">
          <Spinner size="xl" color="primary.500" />
          <Text>Ładowanie zaproszeń...</Text>
        </VStack>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box bg={pageBg} minH="100vh" p={{ base: 4, md: 6 }}>
        <VStack spacing={8} maxW="1200px" mx="auto" align="stretch">
          <HStack spacing={3} flexWrap="wrap">
            <Mail size={32} />
            <Heading size={{ base: "md", md: "lg" }}>Aktywne zaproszenia</Heading>
          </HStack>

          {!hasAny ? (
            <Box bg={cardBg} p={6} rounded="lg" shadow="md" borderWidth="1px" borderColor={borderColor}>
              <VStack spacing={3}>
                <Mail size={48} color="gray" />
                <Text color="neutral.500" textAlign="center" fontSize="lg">
                  Nie masz aktywnych zaproszeń
                </Text>
                <Text color="neutral.400" textAlign="center" fontSize="sm">
                  Kiedy ktoś zaprosi Cię do organizacji lub projektu, zobaczysz to tutaj
                </Text>
              </VStack>
            </Box>
          ) : (
            <Stack spacing={6}>
              {pendingProject.length > 0 && (
                <Box bg={cardBg} p={6} rounded="lg" shadow="md" borderWidth="1px" borderColor={borderColor}>
                  <HStack mb={4}>
                    <FolderKanban size={20} />
                    <Heading size="sm">Zaproszenia do projektów</Heading>
                    <Badge colorScheme="blue">{pendingProject.length}</Badge>
                  </HStack>
                  <Stack spacing={3}>
                    {pendingProject.map((inv) => (
                      <Box key={inv.invitationId} p={4} rounded="lg" border="1px solid" borderColor={borderColor}>
                        <Stack direction={{ base: "column", md: "row" }} justify="space-between" spacing={3}>
                          <VStack align="flex-start" spacing={1}>
                            <Text fontWeight="bold">{inv.projectName}</Text>
                            <Text fontSize="sm" color="neutral.600">Organizacja: {inv.tenantName}</Text>
                            <Text fontSize="sm" color="neutral.500">
                              Od: {inv.invitedByUserName} ({inv.invitedByUserEmail})
                            </Text>
                            {inv.modules.length > 0 && (
                              <Text fontSize="xs" color="neutral.500">
                                Dostęp: {formatModules(inv.modules)}
                              </Text>
                            )}
                            {inv.expiresAt && (
                              <Text fontSize="xs" color="orange.500">
                                Ważne do: {formatDateShort(inv.expiresAt)}
                              </Text>
                            )}
                          </VStack>
                          <Button
                            size="sm"
                            colorScheme="green"
                            onClick={() => void handleAcceptProject(inv)}
                            isLoading={acceptingId === inv.invitationId}
                            isDisabled={acceptingId !== null}
                          >
                            Akceptuj
                          </Button>
                        </Stack>
                      </Box>
                    ))}
                  </Stack>
                </Box>
              )}

              {pendingTenant.length > 0 && (
                <Box bg={cardBg} p={6} rounded="lg" shadow="md" borderWidth="1px" borderColor={borderColor}>
                  <HStack mb={4}>
                    <Mail size={20} />
                    <Heading size="sm">Zaproszenia do organizacji</Heading>
                    <Badge colorScheme="purple">{pendingTenant.length}</Badge>
                  </HStack>
                  <Stack spacing={3}>
                    {pendingTenant.map((invitation) => (
                      <Box key={invitation.invitationId} p={4} rounded="lg" border="1px solid" borderColor={borderColor}>
                        <Stack direction={{ base: "column", md: "row" }} justify="space-between" spacing={3}>
                          <VStack align="flex-start" spacing={1}>
                            <Text fontWeight="bold">{invitation.tenantName}</Text>
                            <Text fontSize="sm" color="neutral.500">
                              Od: {invitation.invitedByUserName} ({invitation.invitedByUserEmail})
                            </Text>
                            {invitation.expiresAt && (
                              <Text fontSize="xs" color="orange.500">
                                Ważne do: {formatDateShort(invitation.expiresAt)}
                              </Text>
                            )}
                          </VStack>
                          <Button
                            size="sm"
                            colorScheme="green"
                            onClick={() =>
                              void handleAcceptTenant(
                                invitation.invitationId,
                                invitation.token,
                                invitation.tenantId
                              )
                            }
                            isLoading={acceptingId === invitation.invitationId}
                            isDisabled={acceptingId !== null}
                          >
                            Akceptuj
                          </Button>
                        </Stack>
                      </Box>
                    ))}
                  </Stack>
                </Box>
              )}
            </Stack>
          )}
        </VStack>
      </Box>
    </MainLayout>
  );
}
