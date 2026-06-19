import { useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import {
  Box,
  Button,
  Heading,
  Spinner,
  Text,
  VStack,
  useColorModeValue,
} from "@chakra-ui/react";
import MainLayout from "../layout/MainLayout";
import { acceptTenantInvitation } from "../services/tenantService";
import { useActiveInvitations, tenantKeys } from "../hooks/queries";
import { useQueryClient } from "@tanstack/react-query";
import { useToastNotification } from "../hooks/useToastNotification";
import { handleApiError } from "../utils/handleApiError";
import { changeActiveTenant } from "../services/tenantService";
import { InvitationStatus } from "../types/auth.types";

export default function AcceptInvitationPage(): React.ReactElement {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { showError, showApiSuccess } = useToastNotification();
  const token = searchParams.get("token") ?? "";
  const type = searchParams.get("type") ?? "tenant";
  const [accepting, setAccepting] = useState(false);

  const { data: invitations = [], isLoading } = useActiveInvitations();

  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.600");

  const invitation = invitations.find(
    (i) => i.token === token && i.status === InvitationStatus.Pending
  );

  useEffect(() => {
    if (!token) {
      navigate("/tenants/invitations", { replace: true });
    }
  }, [token, navigate]);

  const handleAccept = async (): Promise<void> => {
    if (!token) {
      return;
    }

    setAccepting(true);
    try {
      const success = await acceptTenantInvitation(token);
      if (success && invitation) {
        await changeActiveTenant(invitation.tenantId);
        queryClient.invalidateQueries({ queryKey: tenantKeys.invitations() });
        queryClient.invalidateQueries({ queryKey: tenantKeys.my() });
        showApiSuccess("inviteAccepted");

        if (type === "project" && invitation.projectId) {
          navigate(`/projects/${invitation.projectId}`, { replace: true });
        } else {
          navigate("/dashboard", { replace: true });
        }

        return;
      }

      showError("Nie udało się zaakceptować zaproszenia", "Zaproszenie może być nieaktualne lub wygasłe");
    } catch (error) {
      const { title, description } = handleApiError(error);
      showError(title, description);
    } finally {
      setAccepting(false);
    }
  };

  const title =
    invitation?.projectId && invitation.projectName
      ? `Zaproszenie do projektu „${invitation.projectName}”`
      : invitation
        ? `Zaproszenie do organizacji „${invitation.tenantName}”`
        : "Zaproszenie";

  const description =
    invitation?.projectId && invitation.projectName
      ? `Organizacja: ${invitation.tenantName}. Zaproszenie od: ${invitation.invitedByUserName}`
      : invitation
        ? `Zaproszenie od: ${invitation.invitedByUserName}`
        : "Zaakceptuj zaproszenie, aby uzyskać dostęp.";

  return (
    <MainLayout>
      <Box maxW="520px" mx="auto" mt={{ base: 8, md: 16 }} p={4}>
        <Box
          bg={cardBg}
          p={8}
          rounded="lg"
          borderWidth="1px"
          borderColor={borderColor}
          shadow="md"
        >
          {isLoading ? (
            <VStack spacing={4} py={8}>
              <Spinner size="lg" color="primary.500" />
              <Text>Ładowanie zaproszenia...</Text>
            </VStack>
          ) : (
            <VStack spacing={6} align="stretch">
              <Heading size="md">{title}</Heading>
              <Text color="neutral.600">{description}</Text>
              <Button
                colorScheme="primary"
                size="lg"
                onClick={() => void handleAccept()}
                isLoading={accepting}
                loadingText="Akceptowanie..."
              >
                Akceptuj zaproszenie
              </Button>
              <Button variant="ghost" onClick={() => navigate("/tenants/invitations")}>
                Zobacz wszystkie zaproszenia
              </Button>
            </VStack>
          )}
        </Box>
      </Box>
    </MainLayout>
  );
}
