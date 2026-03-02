import { useEffect, useState } from "react";
import {
  Box,
  Heading,
  Text,
  Spinner,
  VStack,
  useColorModeValue,
  Button,
  HStack,
  useToast,
  Stack,
} from "@chakra-ui/react";
import { Mail } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import { getActiveInvitations, acceptTenantInvitation } from "../services/tenantService";
import type { TenantInvitationWeb } from "../types/auth.types";
import { InvitationStatus } from "../types/auth.types";

export default function ActiveInvitations() {
  const [invitations, setInvitations] = useState<TenantInvitationWeb[]>([]);
  const [loading, setLoading] = useState(true);
  const [acceptingInvitationId, setAcceptingInvitationId] = useState<string | null>(null);
  
  const toast = useToast();

  const cardBg = useColorModeValue("white", "gray.800");
  const pageBg = useColorModeValue("gray.50", "gray.900");
  const borderColor = useColorModeValue("gray.200", "gray.600");

  useEffect(() => {
    async function load() {
      try {
        const invitationsData = await getActiveInvitations();
        // Pokazuj tylko zaproszenia Pending
        const pendingInvitations = invitationsData.filter((inv: TenantInvitationWeb) => inv.status === InvitationStatus.Pending);
        setInvitations(pendingInvitations);
      } catch (error) {
      } finally {
        setLoading(false);
      }
    }
    load();
  }, []);

  const handleAcceptInvitation = async (invitationId: string, token: string, tenantName: string) => {
    setAcceptingInvitationId(invitationId);
    try {
      const success = await acceptTenantInvitation(token);
      
      if (success) {
        // Usuń zaproszenie z listy
        setInvitations(prev => prev.filter(inv => inv.invitationId !== invitationId));
        
        toast({
          title: "✅ Zaproszenie zaakceptowane",
          description: `Dołączyłeś do organizacji ${tenantName}`,
          status: "success",
          duration: 4000,
          isClosable: true,
        });
      } else {
        toast({
          title: "Nie udało się zaakceptować zaproszenia",
          description: "Zaproszenie może być nieaktualne lub wygasłe",
          status: "error",
          duration: 5000,
          isClosable: true,
        });
      }
    } catch (error) {
      toast({
        title: "Wystąpił błąd połączenia",
        description: "Sprawdź połączenie internetowe i spróbuj ponownie",
        status: "error",
        duration: 5000,
        isClosable: true,
      });
    } finally {
      setAcceptingInvitationId(null);
    }
  };

  if (loading) {
    return (
      <MainLayout>
        <VStack spacing={4} align="center" justify="center" minH="50vh">
          <Spinner size="xl" color="blue.500" />
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
                <Text color="gray.500" textAlign="center" fontSize="lg">
                  Nie masz aktywnych zaproszeń
                </Text>
                <Text color="gray.400" textAlign="center" fontSize="sm">
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
                            <Text fontSize="sm" color="gray.600">
                              Zaproszenie od:
                            </Text>
                            <Text fontSize="sm" fontWeight="medium">
                              {invitation.invitedByUserName}
                            </Text>
                            <Text fontSize="xs" color="gray.400">
                              ({invitation.invitedByUserEmail})
                            </Text>
                          </HStack>
                          <HStack spacing={2} flexWrap="wrap">
                            <Text fontSize="xs" color="gray.500">
                              Wysłano: {new Date(invitation.createdAt).toLocaleDateString('pl-PL')}
                            </Text>
                            {invitation.expiresAt && (
                              <>
                                <Text fontSize="xs" color="gray.500">
                                  •
                                </Text>
                                <Text fontSize="xs" color="orange.500">
                                  Ważne do: {new Date(invitation.expiresAt).toLocaleDateString('pl-PL')}
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
