import { Box, SimpleGrid, Card, CardBody, Heading, Text, Icon, VStack, Badge, HStack } from "@chakra-ui/react";
import { Building2, Mail, Users, Settings } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { useEffect, useState } from "react";
import MainLayout from "../layout/MainLayout";
import { getActiveInvitations } from "../services/tenantService";
import { InvitationStatus } from "../types/auth.types";

export default function TenantsOverview() {
  const navigate = useNavigate();
  const [invitationsCount, setInvitationsCount] = useState(0);

  useEffect(() => {
    const fetchInvitations = async () => {
      try {
        const invitations = await getActiveInvitations();
        const pending = invitations.filter((inv: { status: number }) => inv.status === InvitationStatus.Pending);
        setInvitationsCount(pending.length);
      } catch (error) {
        console.error("Błąd pobierania zaproszeń:", error);
      }
    };

    fetchInvitations();
  }, []);

  const menuCards = [
    {
      title: "Aktywne zaproszenia",
      description: "Przeglądaj i akceptuj zaproszenia do organizacji",
      icon: Mail,
      color: "red.500",
      path: "/tenants/invitations",
      badge: invitationsCount > 0 ? invitationsCount : undefined,
    },
    {
      title: "Z którymi współpracujesz",
      description: "Organizacje w których jesteś członkiem",
      icon: Users,
      color: "blue.500",
      path: "/tenants/collaborating",
    },
    {
      title: "Którymi zarządzasz",
      description: "Organizacje którymi zarządzasz jako administrator",
      icon: Settings,
      color: "purple.500",
      path: "/tenants/managed",
    },
  ];

  return (
    <MainLayout>
      <Box p={{ base: 4, md: 10 }} minH="100vh">
        <HStack mb={8} spacing={3}>
          <Icon as={Building2} boxSize={8} color="blue.600" />
          <Heading size={{ base: "lg", md: "xl" }}>Organizacje</Heading>
        </HStack>

        <SimpleGrid columns={{ base: 1, md: 2, lg: 3 }} spacing={6}>
          {menuCards.map((card) => (
            <Card
              key={card.title}
              cursor="pointer"
              transition="all 0.2s"
              _hover={{ 
                transform: "translateY(-4px)", 
                shadow: "xl",
                borderColor: card.color 
              }}
              onClick={() => navigate(card.path)}
              borderWidth="2px"
              borderColor="transparent"
            >
              <CardBody>
                <VStack align="flex-start" spacing={4}>
                  <HStack justify="space-between" w="100%">
                    <Icon as={card.icon} boxSize={10} color={card.color} />
                    {card.badge && (
                      <Badge colorScheme="red" fontSize="md" px={3} py={1} borderRadius="full">
                        {card.badge}
                      </Badge>
                    )}
                  </HStack>
                  <VStack align="flex-start" spacing={2}>
                    <Heading size="md">{card.title}</Heading>
                    <Text color="gray.600" fontSize="sm">
                      {card.description}
                    </Text>
                  </VStack>
                </VStack>
              </CardBody>
            </Card>
          ))}
        </SimpleGrid>
      </Box>
    </MainLayout>
  );
}
