import { useState } from "react";
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
} from "@chakra-ui/react";
import { Mail } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import { formatDateShort } from "../utils/formatters";
import { acceptTenantInvitation } from "../services/tenantService";
import { useActiveInvitations, tenantKeys } from "../hooks/queries";
import { useQueryClient } from "@tanstack/react-query";
import { useToastNotification } from "../hooks/useToastNotification";
import { handleApiError } from "../utils/handleApiError";
import type { TenantInvitationWeb } from "../types/auth.types";
import { InvitationStatus } from "../types/auth.types";

export default function ActiveInvitations() {
  const queryClient = useQueryClient();
  const { data: allInvitations = [], isLoading: loading } = useActiveInvitations();
  const invitations = allInvitations.filter(
    (inv) => inv.status === InvitationStatus.Pending
  );
  const [acceptingInvitationId, setAcceptingInvitationId] = useState<string | null>(null);
  
  const { showSuccess, showError, showApiSuccess } = useToastNotification();

  const cardBg = useColorModeValue("white", "gray.800");
  const pageBg = useColorModeValue("gray.50", "gray.900");
  const borderColor = useColorModeValue("gray.200", "gray.600");

  const handleAcceptInvitation = async (invitationId: string, token: string, tenantName: string) => {
    setAcceptingInvitationId(invitationId);
    try {
      const success = await acceptTenantInvitation(token);
      
      if (success) {
        queryClient.invalidateQueries({ queryKey: tenantKeys.invitations() });
        queryClient.invalidateQueries({ queryKey: tenantKeys.my() });
        
        showApiSuccess('inviteAccepted');
      } else {
        showError("Nie udało się zaakceptować zaproszenia", "Zaproszenie może być nieaktualne lub wygasłe");
      }
    } catch (error) {
      console.error("Błąd akceptacji zaproszenia:", error);
      const { title, description } = handleApiError(error);
      showError(title, description);
    } finally {
      setAcceptingInvitationId(null);
    }
  };

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
          {/* Header */}
          <HStack spacing={3} flexWrap="wrap">
            <Mail size={32} />
            <Heading size={{ base: "md", md: "lg" }}>Aktywne zaproszenia</Heading>
          </HStack>

          {/* Lista zaproszeń */}
          {invitations.length === 0 ? (
            <Box bg={cardBg} p={6} rounded="lg" shadow="md" borderWidth="1px" borderColor={borderColor}>
              <VStack spacing={3}>
                <Mail size={48} color="gray" />
                <Text color="neutral.500" textAlign="center" fontSize="lg">
                  Nie masz aktywnych zaproszeń
                </Text>
                <Text color="neutral.400" textAlign="center" fontSize="sm">
                  Kiedy ktoś zaprosi Cię do organizacji, zobaczysz to tutaj
                </Text>
              </VStack>
            </Box>
          ) : (
            <Box bg={cardBg} p={6} rounded="lg" shadow="md" borderWidth="1px" borderColor={borderColor}>
              <Stack spacing={3}>
                {invitations.map((invitation) => (
                  <Box
                    key={invitation.invitationId}
                    p={4}
                    rounded="lg"
                    border="1px solid"
                    borderColor={borderColor}
                    bg="transparent"
                  >
                    <Stack direction={{ base: "column", md: "row" }} justify="space-between" align={{ base: "stretch", md: "center" }} spacing={3}>
                      <VStack align="flex-start" spacing={2} flex={1}>
                        <Text fontWeight="bold" fontSize={{ base: "md", md: "lg" }}>{invitation.tenantName}</Text>
                        <VStack align="flex-start" spacing={1}>
                          <HStack spacing={2}>
                            <Text fontSize="sm" color="neutral.600">
                              Zaproszenie od:
                            </Text>
                            <Text fontSize="sm" fontWeight="medium">
                              {invitation.invitedByUserName}
                            </Text>
                            <Text fontSize="xs" color="neutral.400">
                              ({invitation.invitedByUserEmail})
                            </Text>
                          </HStack>
                          <HStack spacing={2} flexWrap="wrap">
                            <Text fontSize="xs" color="neutral.500">
                              Wysłano: {formatDateShort(invitation.createdAt)}
                            </Text>
                            {invitation.expiresAt && (
                              <>
                                <Text fontSize="xs" color="neutral.500">
                                  •
                                </Text>
                                <Text fontSize="xs" color="orange.500">
                                  Ważne do: {formatDateShort(invitation.expiresAt)}
                                </Text>
                              </>
                            )}
                          </HStack>
                        </VStack>
                      </VStack>
                      <Button
                        size="sm"
                        colorScheme="green"
                        onClick={() => handleAcceptInvitation(
                          invitation.invitationId,
                          invitation.token,
                          invitation.tenantName
                        )}
                        isLoading={acceptingInvitationId === invitation.invitationId}
                        isDisabled={acceptingInvitationId !== null}
                        width={{ base: "100%", md: "auto" }}
                      >
                        Akceptuj
                      </Button>
                    </Stack>
                  </Box>
                ))}
              </Stack>
            </Box>
          )}
        </VStack>
      </Box>
    </MainLayout>
  );
}