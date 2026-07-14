import { Box, SimpleGrid, Card, CardBody, Heading, Text, Icon, VStack, Badge, HStack } from "@chakra-ui/react";
import { Building2, FolderKanban, Settings, Briefcase, RefreshCw, Mail, MessageSquare } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { useState, useEffect } from "react";
import MainLayout from "../layout/MainLayout";
import { getActiveInvitations } from "../services/tenantService";
import { InvitationStatus } from "../types/auth.types";

export default function Dashboard() {
  const navigate = useNavigate();
  const [invitationsCount, setInvitationsCount] = useState(0);

  // Pobierz liczbę aktywnych zaproszeń
  useEffect(() => {
    const fetchInvitations = async () => {
      try {
        const invitations = await getActiveInvitations();
        const pending = invitations.filter((inv: { status: number }) => inv.status === InvitationStatus.Pending);
        setInvitationsCount(pending.length);
      } catch (error) {
      }
    };

    fetchInvitations();
    // Odświeżaj co 30 sekund
    const interval = setInterval(fetchInvitations, 30000);
    return () => clearInterval(interval);
  }, []);

  const menuCards = [
    // Przeniesione do strony Projekty (Select) oraz menu użytkownika
    // {
    //   title: "Przełącz organizację",
    //   description: "Zmień aktywną organizację, z którą współpracujesz",
    //   icon: RefreshCw,
    //   color: "purple.500",
    //   path: "/tenants/collaborating",
    // },
    {
      title: "Projekty",
      description: "Przeglądaj i zarządzaj swoimi projektami",
      icon: FolderKanban,
      color: "level1.500",
      path: "/projects",
    },
    {
      title: "Wiadomości",
      description: "Komunikuj się z członkami projektów i organizacji",
      icon: MessageSquare,
      color: "primary.500",
      path: "/chat",
    },
    {
      title: "Zarządzanie",
      description: "Administruj swoimi organizacjami",
      icon: Building2,
      color: "primary.500",
      path: "/tenants/managed",
    },
    {
      title: "Zaproszenia",
      description: "Zobacz i zaakceptuj zaproszenia do organizacji",
      icon: Mail,
      color: "level2.500",
      path: "/tenants/invitations",
    },
    {
      title: "Zaplanowane prace",
      description: "Zobacz przydzielone do Ciebie zadania",
      icon: Briefcase,
      color: "orange.500",
      path: "/assigned-works",
    },
    {
      title: "Ustawienia",
      description: "Personalizuj swoje konto i preferencje",
      icon: Settings,
      color: "gray.500",
      path: "/profile",
    },
  ];

  return (
    <MainLayout>
      <Box p={{ base: 3, sm: 4, md: 10 }} minH="100vh">
        <Heading mb={{ base: 4, md: 8 }} size={{ base: "md", sm: "lg", md: "xl" }}>
          Panel główny
        </Heading>

        <SimpleGrid columns={{ base: 1, sm: 2, md: 3 }} spacing={{ base: 3, md: 6 }}>
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
              <CardBody p={{ base: 3, md: 6 }}>
                <VStack align="flex-start" spacing={{ base: 3, md: 4 }}>
                  <HStack spacing={2}>
                    <Icon as={card.icon} boxSize={{ base: 8, md: 10 }} color={card.color} />
                    {card.title === "Zaproszenia" && invitationsCount > 0 && (
                      <Badge colorScheme="red" borderRadius="full" fontSize="sm" px={2}>
                        {invitationsCount}
                      </Badge>
                    )}
                  </HStack>
                  <VStack align="flex-start" spacing={1}>
                    <Heading size={{ base: "sm", md: "md" }}>{card.title}</Heading>
                    <Text color="neutral.600" fontSize={{ base: "xs", md: "sm" }}>
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
