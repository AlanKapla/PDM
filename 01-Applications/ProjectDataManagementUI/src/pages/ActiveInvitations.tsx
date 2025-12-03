import { useEffect, useState } from "react";
import { Box, Text, VStack, Flex, Button, Icon, Spinner } from "@chakra-ui/react";
import { Mail } from "lucide-react";
import MainLayout from "../layout/MainLayout";
import { PageHeader } from "../components/PageHeader";
import { getActiveInvitations, acceptTenantInvitation } from "../services/tenantService";
import type { TenantInvitationWeb } from "../types/auth.types";
import { InvitationStatus } from "../types/auth.types";

export default function ActiveInvitations() {
  const [invitations, setInvitations] = useState<TenantInvitationWeb[]>([]);
  const [loading, setLoading] = useState(true);
  const [acceptingId, setAcceptingId] = useState<string | null>(null);

  useEffect(() => {
    const load = async () => {
      try {
        const data = await getActiveInvitations();
        setInvitations(data.filter((x: TenantInvitationWeb) => x.status === InvitationStatus.Pending));
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  const handleAccept = async (inv: TenantInvitationWeb) => {
    setAcceptingId(inv.invitationId);

    try {
      const ok = await acceptTenantInvitation(inv.token);
      if (ok) {
        setInvitations(prev => prev.filter(x => x.invitationId !== inv.invitationId));
      }
    } finally {
      setAcceptingId(null);
    }
  };

  if (loading) {
    return (
      <MainLayout>
        <Flex justify="center" align="center" minH="60vh">
          <Spinner size="xl" color="gray.300" thickness="4px" />
        </Flex>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <Box p={10}>
        <PageHeader
          title="Aktywne zaproszenia"
          breadcrumb={["Organizacje", "Zaproszenia"]}
        />

        {/* BRAK ZAPROSZEŃ */}
        {invitations.length === 0 && (
          <Flex
            direction="column"
            align="center"
            justify="center"
            bg="white"
            border="1px solid"
            borderColor="gray.200"
            rounded="md"
            p={14}
            mt={4}
          >
            <Icon as={Mail} boxSize={20} color="gray.500" mb={4} />
            <Text fontSize="lg" color="gray.300" mb={1}>
              Nie masz aktywnych zaproszeń
            </Text>
            <Text fontSize="sm" color="gray.500">
              Kiedy ktoś Cię zaprosi, zobaczysz to tutaj.
            </Text>
          </Flex>
        )}

        {/* LISTA ZAPROSZEŃ */}
        {invitations.length > 0 && (
          <VStack
            align="stretch"
            spacing={0}
            bg="white"
            border="1px solid"
            borderColor="gray.200"
            rounded="md"
            mt={4}
          >
            {invitations.map(inv => (
              <Flex
                key={inv.invitationId}
                justify="space-between"
                align="center"
                px={5}
                py={4}
                borderBottom="1px solid #1e1e1e"
                _hover={{ bg: "#181818" }}
              >
                <Box>
                  <Text fontSize="md" fontWeight="semibold" color="gray.200">
                    {inv.tenantName}
                  </Text>

                  <Text fontSize="sm" color="gray.400" mt={1}>
                    Zaproszenie od: <span style={{ color: "white" }}>{inv.invitedByUserName}</span>{" "}
                    ({inv.invitedByUserEmail})
                  </Text>

                  <Text fontSize="xs" color="gray.500" mt={1}>
                    Wysłano: {new Date(inv.createdAt).toLocaleDateString("pl-PL")}
                    {inv.expiresAt && (
                      <>
                        {" · "}
                        <span style={{ color: "orange" }}>
                          Ważne do: {new Date(inv.expiresAt).toLocaleDateString("pl-PL")}
                        </span>
                      </>
                    )}
                  </Text>
                </Box>

                <Button
                  bg="green.600"
                  _hover={{ bg: "green.700" }}
                  size="sm"
                  isLoading={acceptingId === inv.invitationId}
                  onClick={() => handleAccept(inv)}
                >
                  Akceptuj
                </Button>
              </Flex>
            ))}
          </VStack>
        )}
      </Box>
    </MainLayout>
  );
}
