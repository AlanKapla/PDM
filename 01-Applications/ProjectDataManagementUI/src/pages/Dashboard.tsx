import { Box, SimpleGrid, Card, CardBody, Heading, Text, Icon, VStack } from "@chakra-ui/react";
import { Building2, FolderKanban, Settings, Briefcase, FileText, RefreshCw, Mail } from "lucide-react";
import { useNavigate } from "react-router-dom";
import MainLayout from "../layout/MainLayout";

export default function Dashboard() {
  const navigate = useNavigate();

  const menuCards = [
    {
      title: "Przełącz organizację",
      description: "Zmień aktywną organizację, z którą współpracujesz",
      icon: RefreshCw,
      color: "purple.500",
      path: "/tenants/collaborating",
    },
    {
      title: "Projekty",
      description: "Przeglądaj i zarządzaj swoimi projektami",
      icon: FolderKanban,
      color: "green.500",
      path: "/projects",
    },
    {
      title: "Zarządzaj",
      description: "Administruj swoimi organizacjami",
      icon: Building2,
      color: "blue.500",
      path: "/tenants/managed",
    },
    {
      title: "Zaproszenia",
      description: "Zobacz i zaakceptuj zaproszenia do organizacji",
      icon: Mail,
      color: "pink.500",
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
      title: "Szablony kosztorysów",
      description: "Zarządzaj szablonami kosztorysów",
      icon: FileText,
      color: "teal.500",
      path: "/cost-estimate-templates",
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
      <Box p={{ base: 4, md: 10 }} minH="100vh">
        <Heading mb={8} size={{ base: "lg", md: "xl" }}>
          Panel główny
        </Heading>

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
                  <Icon as={card.icon} boxSize={10} color={card.color} />
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
